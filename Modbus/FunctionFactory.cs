using Common;
using Modbus.FunctionParameters;
using Modbus.ModbusFunctions;

namespace Modbus
{
    /// <summary>
    /// Factory class for creating modbus functions based on command parameters.
    /// </summary>
    public static class FunctionFactory
    {
        /// <summary>
        /// Creates a modbus function based on the provided command parameters.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        /// <returns>The appropriate modbus function implementation.</returns>
        public static IModbusFunction CreateModbusFunction(ModbusCommandParameters commandParameters)
        {
            switch (commandParameters.FunctionCode)
            {
                case (byte)ModbusFunctionCode.READ_COILS:
                    return new ReadCoilsFunction(commandParameters);

                case (byte)ModbusFunctionCode.READ_DISCRETE_INPUTS:
                    return new ReadDiscreteInputsFunction(commandParameters);

                case (byte)ModbusFunctionCode.READ_HOLDING_REGISTERS:
                    return new ReadHoldingRegistersFunction(commandParameters);

                case (byte)ModbusFunctionCode.READ_INPUT_REGISTERS:
                    return new ReadInputRegistersFunction(commandParameters);

                case (byte)ModbusFunctionCode.WRITE_SINGLE_COIL:
                    return new WriteSingleCoilFunction(commandParameters);

                case (byte)ModbusFunctionCode.WRITE_SINGLE_REGISTER:
                    return new WriteSingleRegisterFunction(commandParameters);

                default:
                    throw new System.ArgumentException($"Unsupported function code: {commandParameters.FunctionCode}");
            }
        }
    }
}