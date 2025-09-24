using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    public class WriteSingleCoilFunction : ModbusFunction
    {
        public WriteSingleCoilFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

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
            // Output Address (2 bajta)
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)writeParams.OutputAddress)), 0, request, 8, 2);

            // Output Value (2 bajta) - Za coils: 0x0000 = OFF, 0xFF00 = ON
            ushort coilValue = (ushort)(writeParams.Value == 0 ? 0x0000 : 0xFF00);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)coilValue)), 0, request, 10, 2);

            

            return request;
        }

        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            ModbusWriteCommandParameters writeParams = CommandParameters as ModbusWriteCommandParameters;
            Dictionary<Tuple<PointType, ushort>, ushort> result = new Dictionary<Tuple<PointType, ushort>, ushort>();


            result.Add(
                new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, writeParams.OutputAddress),
                writeParams.Value
            );

            return result;
        }
    }
}