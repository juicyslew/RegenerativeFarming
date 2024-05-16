using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegenerativeAgriculture
{
    public class SoilSurveyorData
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Texture { get; set; } = "Juicyslew.RegenerativeAgriculture/soil_surveyor_placeholder.png";
        public int TextureIndex { get; set; } = 5;

        //public string? ConventionalUpgradeFrom { get; set; } = null;

        public int UpgradeLevel { get; set; } = 0;
    }
}
