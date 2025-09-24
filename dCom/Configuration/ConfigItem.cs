using Common;
using System;
using System.Collections.Generic;

namespace dCom.Configuration
{
    internal class ConfigItem : IConfigItem
    {
        #region Fields

        private PointType registryType;
        private ushort numberOfRegisters;
        private ushort startAddress;
        private ushort decimalSeparatorPlace;
        private ushort minValue;
        private ushort maxValue;
        private ushort defaultValue;
        private string processingType;
        private string description;
        private int acquisitionInterval;
        private double scalingFactor;
        private double deviation;
        private double egu_max;
        private double egu_min;
        private ushort abnormalValue;
        private double highLimit;
        private double lowLimit;
        private int secondsPassedSinceLastPoll;

        #endregion Fields

        #region Properties

        public PointType RegistryType
        {
            get
            {
                return registryType;
            }

            set
            {
                registryType = value;
            }
        }

        public ushort NumberOfRegisters
        {
            get
            {
                return numberOfRegisters;
            }

            set
            {
                numberOfRegisters = value;
            }
        }

        public ushort StartAddress
        {
            get
            {
                return startAddress;
            }

            set
            {
                startAddress = value;
            }
        }

        public ushort DecimalSeparatorPlace
        {
            get
            {
                return decimalSeparatorPlace;
            }

            set
            {
                decimalSeparatorPlace = value;
            }
        }

        public ushort MinValue
        {
            get
            {
                return minValue;
            }

            set
            {
                minValue = value;
            }
        }

        public ushort MaxValue
        {
            get
            {
                return maxValue;
            }

            set
            {
                maxValue = value;
            }
        }

        public ushort DefaultValue
        {
            get
            {
                return defaultValue;
            }

            set
            {
                defaultValue = value;
            }
        }

        public string ProcessingType
        {
            get
            {
                return processingType;
            }

            set
            {
                processingType = value;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }

            set
            {
                description = value;
            }
        }

        public int AcquisitionInterval
        {
            get
            {
                return acquisitionInterval;
            }

            set
            {
                acquisitionInterval = value;
            }
        }

        public double ScaleFactor
        {
            get
            {
                return scalingFactor;
            }
            set
            {
                scalingFactor = value;
            }
        }

        public double Deviation
        {
            get
            {
                return deviation;
            }

            set
            {
                deviation = value;
            }
        }

        public double EGU_Max
        {
            get
            {
                return egu_max;
            }

            set
            {
                egu_max = value;
            }
        }

        public double EGU_Min
        {
            get
            {
                return egu_min;
            }

            set
            {
                egu_min = value;
            }
        }

        public ushort AbnormalValue
        {
            get
            {
                return abnormalValue;
            }

            set
            {
                abnormalValue = value;
            }
        }

        public double HighLimit
        {
            get
            {
                return highLimit;
            }

            set
            {
                highLimit = value;
            }
        }

        public double LowLimit
        {
            get
            {
                return lowLimit;
            }

            set
            {
                lowLimit = value;
            }
        }

        public int SecondsPassedSinceLastPoll
        {
            get
            {
                return secondsPassedSinceLastPoll;
            }

            set
            {
                secondsPassedSinceLastPoll = value;
            }
        }

        #endregion Properties

        public ConfigItem(List<string> configurationParameters)
        {
            RegistryType = GetRegistryType(configurationParameters[0]);
            int temp;
            double doubleTemp;

            // Parse basic parameters (existing logic)
            Int32.TryParse(configurationParameters[1], out temp);
            NumberOfRegisters = (ushort)temp;
            Int32.TryParse(configurationParameters[2], out temp);
            StartAddress = (ushort)temp;
            Int32.TryParse(configurationParameters[3], out temp);
            DecimalSeparatorPlace = (ushort)temp;
            Int32.TryParse(configurationParameters[4], out temp);
            MinValue = (ushort)temp;
            Int32.TryParse(configurationParameters[5], out temp);
            MaxValue = (ushort)temp;
            Int32.TryParse(configurationParameters[6], out temp);
            DefaultValue = (ushort)temp;
            ProcessingType = configurationParameters[7];
            Description = configurationParameters[8].TrimStart('@');

            // Parse acquisition interval
            if (configurationParameters[9].Equals("#"))
            {
                AcquisitionInterval = 1;
            }
            else
            {
                Int32.TryParse(configurationParameters[9], out temp);
                AcquisitionInterval = temp;
            }

            // Parse new parameters based on point type
            if (RegistryType == PointType.ANALOG_INPUT || RegistryType == PointType.ANALOG_OUTPUT)
            {
                // Analog points: A B EGU_Min EGU_Max HighAlarm LowAlarm
                if (configurationParameters.Count >= 16)
                {
                    // A (Scaling Factor) - parameter 10
                    double.TryParse(configurationParameters[10], out doubleTemp);
                    ScaleFactor = doubleTemp != 0 ? doubleTemp : 1.0; // Default to 1 if not specified or 0

                    // B (Deviation) - parameter 11
                    double.TryParse(configurationParameters[11], out doubleTemp);
                    Deviation = doubleTemp; // Default to 0

                    // EGU_Min - parameter 12
                    double.TryParse(configurationParameters[12], out doubleTemp);
                    EGU_Min = doubleTemp;

                    // EGU_Max - parameter 13
                    double.TryParse(configurationParameters[13], out doubleTemp);
                    EGU_Max = doubleTemp;

                    // HighAlarm - parameter 14
                    double.TryParse(configurationParameters[14], out doubleTemp);
                    HighLimit = doubleTemp;

                    // LowAlarm - parameter 15
                    double.TryParse(configurationParameters[15], out doubleTemp);
                    LowLimit = doubleTemp;
                }
                else
                {
                    // Set defaults for analog points if parameters missing
                    ScaleFactor = 1.0;
                    Deviation = 0.0;
                    EGU_Min = 0.0;
                    EGU_Max = 65535.0;
                    HighLimit = 60000.0;
                    LowLimit = 1000.0;
                }

                // AbnormalValue not applicable for analog points
                AbnormalValue = 0;
            }
            else if (RegistryType == PointType.DIGITAL_INPUT || RegistryType == PointType.DIGITAL_OUTPUT)
            {
                // Digital points: AbnormalValue
                if (configurationParameters.Count >= 11)
                {
                    // AbnormalValue - parameter 10
                    Int32.TryParse(configurationParameters[10], out temp);
                    AbnormalValue = (ushort)temp;
                }
                else
                {
                    // Default abnormal value for digital points
                    // Specification says nominal state is OFF (0), so abnormal is ON (1)
                    AbnormalValue = 1;
                }

                // Set defaults for unused analog parameters
                ScaleFactor = 1.0;
                Deviation = 0.0;
                EGU_Min = 0.0;
                EGU_Max = 1.0;
                HighLimit = 0.0;
                LowLimit = 0.0;
            }
            else
            {
                // Other point types - set defaults
                ScaleFactor = 1.0;
                Deviation = 0.0;
                EGU_Min = 0.0;
                EGU_Max = 65535.0;
                HighLimit = 60000.0;
                LowLimit = 1000.0;
                AbnormalValue = 0;
            }
        }

        private PointType GetRegistryType(string registryTypeName)
        {
            PointType registryType;
            switch (registryTypeName)
            {
                case "DO_REG":
                    registryType = PointType.DIGITAL_OUTPUT;
                    break;

                case "DI_REG":
                    registryType = PointType.DIGITAL_INPUT;
                    break;

                case "IN_REG":
                    registryType = PointType.ANALOG_INPUT;
                    break;

                case "HR_INT":
                    registryType = PointType.ANALOG_OUTPUT;
                    break;

                default:
                    registryType = PointType.HR_LONG;
                    break;
            }
            return registryType;
        }
    }
}