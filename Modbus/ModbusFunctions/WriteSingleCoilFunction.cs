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

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)writeParams.TransactionId)), 0, request, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)writeParams.ProtocolId)), 0, request, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)writeParams.Length)), 0, request, 4, 2);
            request[6] = writeParams.UnitId;
            request[7] = writeParams.FunctionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)writeParams.OutputAddress)), 0, request, 8, 2);

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