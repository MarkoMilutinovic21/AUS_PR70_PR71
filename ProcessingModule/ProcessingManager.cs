using Common;
using Modbus;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for processing points and executing commands.
    /// </summary>
    public class ProcessingManager : IProcessingManager
    {
        private IFunctionExecutor functionExecutor;
        private IStorage storage;
        private AlarmProcessor alarmProcessor;
        private EGUConverter eguConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessingManager"/> class.
        /// </summary>
        /// <param name="storage">The point storage.</param>
        /// <param name="functionExecutor">The function executor.</param>
        public ProcessingManager(IStorage storage, IFunctionExecutor functionExecutor)
        {
            this.storage = storage;
            this.functionExecutor = functionExecutor;
            this.alarmProcessor = new AlarmProcessor();
            this.eguConverter = new EGUConverter();
            this.functionExecutor.UpdatePointEvent += CommandExecutor_UpdatePointEvent;
        }

        /// <inheritdoc />
        public void ExecuteReadCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort startAddress, ushort numberOfPoints)
        {
            try
            {
                ModbusFunctionCode? functionCode = GetReadFunctionCode(configItem.RegistryType);
                if (functionCode == null)
                {
                    throw new ArgumentException($"Unsupported registry type for reading: {configItem.RegistryType}");
                }

                ModbusReadCommandParameters p = new ModbusReadCommandParameters(6, (byte)functionCode, startAddress, numberOfPoints, transactionId, remoteUnitAddress);
                IModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
                this.functionExecutor.EnqueueCommand(fn);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing read command: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public void ExecuteWriteCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {
            try
            {
                if (configItem.RegistryType == PointType.ANALOG_OUTPUT || configItem.RegistryType == PointType.HR_LONG)
                {
                    ExecuteAnalogCommand(configItem, transactionId, remoteUnitAddress, pointAddress, value);
                }
                else if (configItem.RegistryType == PointType.DIGITAL_OUTPUT)
                {
                    ExecuteDigitalCommand(configItem, transactionId, remoteUnitAddress, pointAddress, value);
                }
                else
                {
                    throw new ArgumentException($"Cannot write to registry type: {configItem.RegistryType}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing write command: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Executes a digital write command.
        /// </summary>
        /// <param name="configItem">The configuration item.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <param name="remoteUnitAddress">The remote unit address.</param>
        /// <param name="pointAddress">The point address.</param>
        /// <param name="value">The value.</param>
        private void ExecuteDigitalCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {
            // Ensure value is 0 or 1 for digital points
            ushort digitalValue = (ushort)(value != 0 ? 1 : 0);

            ModbusWriteCommandParameters p = new ModbusWriteCommandParameters(6, (byte)ModbusFunctionCode.WRITE_SINGLE_COIL, pointAddress, digitalValue, transactionId, remoteUnitAddress);
            IModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
            this.functionExecutor.EnqueueCommand(fn);
        }

        /// <summary>
        /// Executes an analog write command.
        /// </summary>
        /// <param name="configItem">The configuration item.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <param name="remoteUnitAddress">The remote unit address.</param>
        /// <param name="pointAddress">The point address.</param>
        /// <param name="value">The value.</param>
        private void ExecuteAnalogCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {
            ushort rawValue;

            // Check if value needs EGU to raw conversion
            if (Math.Abs(value) > ushort.MaxValue || (value > 1000 && configItem.ScaleFactor != 1.0))
            {
                // Value is likely in EGU units, convert to raw using inverse formula
                // raw = (EGU - B) / A
                rawValue = eguConverter.ConvertToRaw(configItem.ScaleFactor, configItem.Deviation, value);
            }
            else
            {
                // Value is already in raw format or small enough to be raw
                rawValue = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, value));
            }

            ModbusWriteCommandParameters p = new ModbusWriteCommandParameters(6, (byte)ModbusFunctionCode.WRITE_SINGLE_REGISTER, pointAddress, rawValue, transactionId, remoteUnitAddress);
            IModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
            this.functionExecutor.EnqueueCommand(fn);
        }

        /// <summary>
        /// Gets the modbus function code for the point type.
        /// </summary>
        /// <param name="registryType">The register type.</param>
        /// <returns>The modbus function code.</returns>
        private ModbusFunctionCode? GetReadFunctionCode(PointType registryType)
        {
            switch (registryType)
            {
                case PointType.DIGITAL_OUTPUT:
                    return ModbusFunctionCode.READ_COILS;
                case PointType.DIGITAL_INPUT:
                    return ModbusFunctionCode.READ_DISCRETE_INPUTS;
                case PointType.ANALOG_INPUT:
                    return ModbusFunctionCode.READ_INPUT_REGISTERS;
                case PointType.ANALOG_OUTPUT:
                    return ModbusFunctionCode.READ_HOLDING_REGISTERS;
                case PointType.HR_LONG:
                    return ModbusFunctionCode.READ_HOLDING_REGISTERS;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Method for handling received points.
        /// </summary>
        /// <param name="type">The point type.</param>
        /// <param name="pointAddress">The point address.</param>
        /// <param name="newValue">The new value.</param>
        private void CommandExecutor_UpdatePointEvent(PointType type, ushort pointAddress, ushort newValue)
        {
            try
            {
                List<IPoint> points = storage.GetPoints(new List<PointIdentifier>(1) { new PointIdentifier(type, pointAddress) });

                if (points.Count > 0)
                {
                    var point = points.First();

                    if (type == PointType.ANALOG_INPUT || type == PointType.ANALOG_OUTPUT || type == PointType.HR_LONG)
                    {
                        ProcessAnalogPoint(point as IAnalogPoint, newValue);
                    }
                    else if (type == PointType.DIGITAL_INPUT || type == PointType.DIGITAL_OUTPUT)
                    {
                        ProcessDigitalPoint(point as IDigitalPoint, newValue);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing point update for address {pointAddress}: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a digital point with alarm checking.
        /// </summary>
        /// <param name="point">The digital point</param>
        /// <param name="newValue">The new value.</param>
        /// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * 

        private void ProcessDigitalPoint(IDigitalPoint point, ushort newValue)
        {
            if (point == null) return;

            try
            {
                // Update basic properties
                point.RawValue = newValue;
                point.Timestamp = DateTime.Now;
                point.State = (DState)(newValue != 0 ? 1 : 0);

                // Process alarm for digital point
                AlarmType alarm = alarmProcessor.GetAlarmForDigitalPoint(newValue, point.ConfigItem);
                point.Alarm = alarm;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing digital point: {ex.Message}");
                point.Alarm = AlarmType.ABNORMAL_VALUE;
            }
        }

        /// <summary>
        /// Processes an analog point with EGU conversion and alarm checking.
        /// </summary>
        /// <param name="point">The analog point.</param>
        /// <param name="newValue">The new value.</param>
        /// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * 
        private void ProcessAnalogPoint(IAnalogPoint point, ushort newValue)
        {
            if (point == null) return;

            try
            {
                // Update basic properties
                point.RawValue = newValue;
                point.Timestamp = DateTime.Now;

                // Convert raw value to EGU using the formula: EGU = A * raw + B
                double eguValue = eguConverter.ConvertToEGU(
                    point.ConfigItem.ScaleFactor,
                    point.ConfigItem.Deviation,
                    newValue);

                point.EguValue = eguValue;

                // Process alarm for analog point using EGU value
                AlarmType alarm = alarmProcessor.GetAlarmForAnalogPoint(eguValue, point.ConfigItem);
                point.Alarm = alarm;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing analog point: {ex.Message}");
                point.Alarm = AlarmType.REASONABILITY_FAILURE;
            }
        }

        /// <inheritdoc />
        public void InitializePoint(PointType type, ushort pointAddress, ushort defaultValue)
        {
            try
            {
                List<IPoint> points = storage.GetPoints(new List<PointIdentifier>(1) { new PointIdentifier(type, pointAddress) });

                if (points.Count > 0)
                {
                    var point = points.First();

                    if (type == PointType.ANALOG_INPUT || type == PointType.ANALOG_OUTPUT || type == PointType.HR_LONG)
                    {
                        ProcessAnalogPoint(point as IAnalogPoint, defaultValue);
                    }
                    else if (type == PointType.DIGITAL_INPUT || type == PointType.DIGITAL_OUTPUT)
                    {
                        ProcessDigitalPoint(point as IDigitalPoint, defaultValue);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing point {pointAddress}: {ex.Message}");
            }
        }
    }
}