using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using BizTalkComponents.Utils;
using Microsoft.BizTalk.Component.Interop;
using Microsoft.BizTalk.Message.Interop;
using Microsoft.XLANGs.RuntimeTypes;
using IComponent = Microsoft.BizTalk.Component.Interop.IComponent;

namespace BizTalkComponents.PipelineComponents.HL7MessagePartCreator
{
    [ComponentCategory(CategoryTypes.CATID_PipelineComponent)]
    [System.Runtime.InteropServices.Guid("23E5CD5D-B0D6-4546-8E86-05038ACE8542")]
    [ComponentCategory(CategoryTypes.CATID_Any)]
    public partial class HL7MessagePartCreator : IBaseComponent, IPersistPropertyBag, IComponent, IComponentUI
    {
        [Description("The document spec name of the MSH schema.")]
        [DisplayName("DocumentSpecName")]
        [RequiredRuntime]
        public string DocumentSpecName { get; set; }

        private const string DocumentSpecNamePropertyName = "DocumentSpecName";

        public IBaseMessage Execute(IPipelineContext pContext, IBaseMessage pInMsg)
        {
            string errorMessage;

            if (!Validate(out errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            var bodyPart = pContext.GetMessageFactory().CreateMessagePart();
            bodyPart.Data = pInMsg.BodyPart.Data;
            bodyPart.Charset = "utf-8";
            bodyPart.ContentType = null;

            var mshPart = CreateMSHSegment(pInMsg.Context, pContext);
            mshPart.Charset = "utf-8";
            mshPart.ContentType = null;

            var zPart = CreateZSegment(pContext);
            zPart.Charset = "utf-8";
            pInMsg.RemovePart("Body");
            pInMsg.AddPart("MSHSegment", mshPart, false);
            pInMsg.AddPart("BodySegments", bodyPart, true);
            pInMsg.AddPart("ZSegments", zPart, false);
            pInMsg.Context = pInMsg.Context;

            return pInMsg;
        }

        private IBaseMessagePart CreateMSHSegment(IBaseMessageContext ctx, IPipelineContext pCtx)
        {
            //Get a reference to the BizTalk schema.
            var documentSpec = (DocumentSpec)pCtx.GetDocumentSpecByName(DocumentSpecName);

            //Get a list of properties defined in the schema.
            var annotations = documentSpec.GetPropertyAnnotationEnumerator();

            var sw = new StringWriter(new StringBuilder());

            //Create a new instance of the schema.
            var doc = XDocument.Load(documentSpec.CreateXmlInstance(sw));
            sw.Dispose();

            //Write all properties to the message body.
            while (annotations.MoveNext())
            {
                var annotation = (IPropertyAnnotation)annotations.Current;
                var node = doc.XPathSelectElement(annotation.XPath);
                object propertyValue;

                if (ctx.TryRead(new ContextProperty(annotation.Name, annotation.Namespace), out propertyValue))
                {
                    node.Value = propertyValue.ToString();
                }
            }

            //Make sure to remove any empty nodes.
            doc.Descendants().Where(e => string.IsNullOrEmpty(e.Value)).Remove();

            var ms = new MemoryStream();
            var xws = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true };
            using (var w = XmlWriter.Create(ms, xws))
            {
                doc.Save(w);
            }

            var mshPart = pCtx.GetMessageFactory().CreateMessagePart();

            mshPart.Data = ms;
            ms.Seek(0, SeekOrigin.Begin);
            ms.Position = 0;

            return mshPart;
        }

        private IBaseMessagePart CreateZSegment(IPipelineContext pCtx)
        {
            var part = pCtx.GetMessageFactory().CreateMessagePart();
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(string.Empty);
            writer.Flush();
            ms.Position = 0;

            part.Data = ms;
            return part;
        }

        public void Load(IPropertyBag propertyBag, int errorLog)
        {
            DocumentSpecName = PropertyBagHelper.ToStringOrDefault(PropertyBagHelper.ReadPropertyBag(propertyBag, DocumentSpecNamePropertyName), string.Empty);
        }

        public void Save(IPropertyBag propertyBag, bool clearDirty, bool saveAllProperties)
        {
            PropertyBagHelper.WritePropertyBag(propertyBag, DocumentSpecNamePropertyName, DocumentSpecName);
        }
    }
}
