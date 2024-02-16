using System;
using System.Globalization;

namespace MSFS2020_AutoFPS
{
    public class RealInvariantFormat : IFormatProvider
    {
        public NumberFormatInfo formatInfo = CultureInfo.InvariantCulture.NumberFormat;

        public RealInvariantFormat(string value)
        {
            if (value == null)
            {
                formatInfo = new CultureInfo("en-US").NumberFormat;
                return;
            }

            int lastPoint = value.LastIndexOf('.');
            int lastComma = value.LastIndexOf(',');
            if (lastComma > lastPoint)
            {
                formatInfo = new CultureInfo("de-DE").NumberFormat;
            }
            else
            {
                formatInfo = new CultureInfo("en-US").NumberFormat;
            }
        }

        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(NumberFormatInfo))
            {
                return formatInfo;
            }
            else
                return null;
        }
    }
}
