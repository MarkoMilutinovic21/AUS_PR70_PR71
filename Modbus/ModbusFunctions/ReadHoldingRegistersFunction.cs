using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read holding registers functions/requests.
    /// </summary>
    public class ReadHoldingRegistersFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadHoldingRegistersFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadHoldingRegistersFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            ModbusReadCommandParameters readParams = CommandParameters as ModbusReadCommandParameters;
            byte[] request = new byte[12];

            // Transaction ID (2 bajta)
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.TransactionId)), 0, request, 0, 2);
            // Protocol ID (2 bajta)
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.ProtocolId)), 0, request, 2, 2);
            // Length (2 bajta)
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.Length)), 0, request, 4, 2);
            // Unit ID (1 bajt)
            request[6] = readParams.UnitId;
            // Function Code (1 bajt)
            request[7] = readParams.FunctionCode;
            // Start Address (2 bajta)
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.StartAddress)), 0, request, 8, 2);
            // Quantity (2 bajta)
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.Quantity)), 0, request, 10, 2);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            ModbusReadCommandParameters readParams = CommandParameters as ModbusReadCommandParameters;
            Dictionary<Tuple<PointType, ushort>, ushort> result = new Dictionary<Tuple<PointType, ushort>, ushort>();

            ushort address = readParams.StartAddress;

            // response[8] = broj bajtova sa podacima (svaki register = 2 bajta)
            for (int i = 0; i < response[8] / 2; i++)
            {
                // Čitamo 2 bajta za svaki register (big endian format)
                byte highByte = response[9 + i * 2];
                byte lowByte = response[10 + i * 2];
                ushort value = BitConverter.ToUInt16(new byte[2] { lowByte, highByte }, 0);

                // Dodaj kao ANALOG_OUTPUT (holding registers)
                result.Add(
                    new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, address),
                    value
                );

                address++;
            }

            return result;
        }
    }
}