using System;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for engineering unit conversion.
    /// Formula applied to all analog outputs:
    /// EGU_value = scalingFactor * raw_value + deviation
    /// </summary>
    public class EGUConverter
    {
        /// <summary>
        /// Converts a raw analog value to EGU (engineering units)
        /// using the formula: EGU_value = scalingFactor * rawValue + deviation
        /// </summary>
        /// <param name="scalingFactor">Scaling factor (A)</param>
        /// <param name="deviation">Deviation (B)</param>
        /// <param name="rawValue">Raw analog value</param>
        /// <returns>Value converted to engineering units</returns>
        public double ConvertToEGU(double scalingFactor, double deviation, ushort rawValue)
        {
            // 1. Uzmi ulaznu raw vrednost (rawValue)
            // 2. Pomnoži sa skalirajućim faktorom (scalingFactor, tj. A)
            // 3. Dodaj devijaciju (deviation, tj. B)
            // 4. Rezultat je EGU vrednost
            double eguValue = scalingFactor * rawValue + deviation;

            // 5. Vrati izračunatu EGU vrednost
            return eguValue;
        }

        /// <summary>
        /// Converts an EGU value back to raw form
        /// using the inverse of the formula: rawValue = (EGU_value - deviation) / scalingFactor
        /// </summary>
        /// <param name="scalingFactor">Scaling factor (A)</param>
        /// <param name="deviation">Deviation (B)</param>
        /// <param name="eguValue">Value in engineering units</param>
        /// <returns>Corresponding raw value</returns>
        public ushort ConvertToRaw(double scalingFactor, double deviation, double eguValue)
        {
            // 1. Uzmi EGU vrednost
            // 2. Oduzmi devijaciju (B)
            // 3. Podeli rezultat sa skalirajućim faktorom (A)
            // 4. Rezultat je raw vrednost
            double rawValue = (eguValue - deviation) / scalingFactor;

            // 5. Pretvori u ushort i vrati
            return (ushort)rawValue;
        }
    }
}
