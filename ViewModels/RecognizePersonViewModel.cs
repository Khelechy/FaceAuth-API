using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceAuth.ViewModels
{
    public class RecognizePersonViewModel
    {
        public string Image { get; set; }
        public string GroupId { get; set; }
    }
}
