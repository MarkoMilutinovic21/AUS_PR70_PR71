// WriteSingleRegisterFunction.cs - Kompletna implementacija  
using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write single register functions/requests.
    /// </summary>
    public class WriteSingleRegisterFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleRegisterFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleRegisterFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            ModbusWriteCommandParameters writeParams = CommandParameters as ModbusWriteCommandParameters;

            byte[] request = new byte[12];

            // Transaction ID (2 bajta)
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)writeParams.TransactionId)), 0, request, 0, 2);
            // Protocol ID (2 bajta)  
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)writeParams.ProtocolId)), 0, request, 2, 2);
            // Length (2 bajta)
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)writeParams.Length)), 0, request, 4, 2);
            // Unit ID (1 bajt)
            request[6] = writeParams.UnitId;
            // Function Code (1 bajt)
            request[7] = writeParams.FunctionCode;
            // Register Address (2 bajta)
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)writeParams.OutputAddress)), 0, request, 8, 2);
            // Register Value (2 bajta)
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)writeParams.Value)), 0, request, 10, 2);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            ModbusWriteCommandParameters writeParams = CommandParameters as ModbusWriteCommandParameters;
            Dictionary<Tuple<PointType, ushort>, ushort> result = new Dictionary<Tuple<PointType, ushort>, ushort>();

            // Za write komande, vraćamo potvrdu uspešnog pisanja
            result.Add(
                new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, writeParams.OutputAddress),
                writeParams.Value
            );

            return result;
        }
    }
}