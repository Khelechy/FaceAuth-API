using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceAuth.ResponseModels
{
    public class LargePersonGroupResponse
    {

        public string largePersonGroupId { get; set; }
        public string name { get; set; }
        public string userData { get; set; }
        public string recognitionModel { get; set; }

    }
}
