using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read coil functions/requests.
    /// </summary>
    public class ReadCoilsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadCoilsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
		public ReadCoilsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc/>
        public override byte[] PackRequest()
        {
            ModbusReadCommandParameters paranCon = this.CommandParameters as ModbusReadCommandParameters;

            byte[] request = new byte[12];

            Buffer.BlockCopy(
                    (Array)BitConverter.GetBytes(               // pretvara short u niz od 2 bajta
                        IPAddress.HostToNetworkOrder(           // little u big endian
                                (short)paranCon.TransactionId)  // sta kopiramo
                    ),
                    0,                  // odakle kopira
                    (Array)request,     // gde kopira
                    0,                  // na koje mesto kopira
                    2);                 // koliko bajta kopira

            Buffer.BlockCopy((Array)BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)paranCon.ProtocolId)), 0, (Array)request, 2, 2);
            Buffer.BlockCopy((Array)BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)paranCon.Length)), 0, (Array)request, 4, 2);
            request[6] = paranCon.UnitId;
            request[7] = paranCon.FunctionCode;
            Buffer.BlockCopy((Array)BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)paranCon.StartAddress)), 0, (Array)request, 8, 2);
            Buffer.BlockCopy((Array)BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)paranCon.Quantity)), 0, (Array)request, 10, 2);

            return request; // ovo iznad ide po slici 

            //throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            // Preuzimamo parametre komande (StartAddress, Quantity, itd.)
            ModbusReadCommandParameters paramCon = this.CommandParameters as ModbusReadCommandParameters;

            // Recnik u koji smestamo rezultat:
            //   kljuc = (Tip tacke, Adresa)
            //   vrednost = stanje (0 ili 1)
            Dictionary<Tuple<PointType, ushort>, ushort> d = new Dictionary<Tuple<PointType, ushort>, ushort>();

            // response[8] = broj bajtova koji sadrže podatke (koliko sledecih bajtova ima bitove za vrednosti)
            int q = response[8];

            // Svaki bajt sadrži 8 digitalnih vrednosti (bitova)
            for (int i = 0; i < q; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    // Ako smo procitali sve trazene Quantity tacke -> prekidamo
                    if ((j + i * 8) >= paramCon.Quantity)
                    {
                        break;
                    }

                    // Uzimamo poslednji (najmanje znacajni) bit iz trenutnog bajta
                    ushort v = (ushort)(response[9 + i] & 0x01);    // ovo mu govori da je neki digitalni i jedan i drugi i ulaz i izlaz su digitalni

                    // Pomeri bajt udesno za jedan bit, da sledeci put uzmemo sledeci bit
                    response[9 + i] /= 1;   // moze i >>= 

                    // Dodaj u recnik:
                    // - Tip tacke (DIGITAL_OUTPUT)
                    // - Adresa = StartAddress + pomeraj
                    // - Vrednost (0 ili 1)
                    d.Add(                                  // OVDE JE JEDINO GDE NAVODI DA JE OVO DIGITALNI OUTPUT
                        new Tuple<PointType, ushort>(           // kad jednom implementira
                            PointType.DIGITAL_OUTPUT,
                            (ushort)(paramCon.StartAddress + (j + i * 8))
                        ),
                        v
                    );
                }
            }

            // vraca mapu procitanih vrednosti
            return d;
        }

    }
}