using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace JdaTeams.Connector.Mappings
{
    public interface IShiftThemeMap
    {
        string MapTheme(string themeCode);
    }
}
