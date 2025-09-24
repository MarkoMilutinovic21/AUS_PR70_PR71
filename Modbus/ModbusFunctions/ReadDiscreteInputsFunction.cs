// ReadDiscreteInputsFunction.cs - Kompletna implementacija
using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read discrete inputs functions/requests.
    /// </summary>
    public class ReadDiscreteInputsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadDiscreteInputsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadDiscreteInputsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
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

            // response[8] = broj bajtova sa podacima
            int byteCount = response[8];

            // Parsiranje digitalnih ulaza (slično kao coils)
            for (int i = 0; i < byteCount; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    // Ako smo procitali sve tražene tacke
                    if ((j + i * 8) >= readParams.Quantity)
                    {
                        break;
                    }

                    // Uzmi bit iz trenutnog bajta
                    ushort value = (ushort)(response[9 + i] & 0x01);

                    // Pomeri bajt za sledeci bit
                    response[9 + i] >>= 1;

                    // Dodaj u rezultat kao DIGITAL_INPUT
                    result.Add(
                        new Tuple<PointType, ushort>(
                            PointType.DIGITAL_INPUT,
                            (ushort)(readParams.StartAddress + (j + i * 8))
                        ),
                        value
                    );
                }
            }

            return result;
        }
    }
}