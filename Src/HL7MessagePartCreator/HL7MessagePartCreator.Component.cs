using System;
using System.Collections;
using System.Linq;
using BizTalkComponents.Utils;

namespace BizTalkComponents.PipelineComponents.HL7MessagePartCreator
{
    public partial class HL7MessagePartCreator
    {
        public string Description
        {
            get { return "Creates MSH and Z segments for HL7 messages."; }
        }

        public string Name
        {
            get { return "HL7MessagePartsCreator"; }
        }

        public string Version
        {
            get { return "1.0"; }
        }

        public void GetClassID(out Guid classID)
        {
            classID = Guid.Parse("DACFA6D5-EA1F-47A4-A74C-58C3C010F8EF");
        }

        public void InitNew()
        {
        }

        public IEnumerator Validate(object projectSystem)
        {
            return ValidationHelper.Validate(this, false).ToArray().GetEnumerator();
        }

        public bool Validate(out string errorMessage)
        {
            var errors = ValidationHelper.Validate(this, true).ToArray();

            if (errors.Any())
            {
                errorMessage = string.Join(",", errors);

                return false;
            }

            errorMessage = string.Empty;

            return true;
        }

        public IntPtr Icon { get { return IntPtr.Zero; } }
    }
}