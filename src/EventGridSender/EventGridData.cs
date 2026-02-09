using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventGrid.Namespaces;
using Claros.Common.Core;
using Claros.Instrument.Core;
using Claros.IoT.Registry;
using Google.Protobuf;
using System.Text;
using CloudEvent = Azure.Messaging.CloudEvent;

namespace EventGridSender
{
    public static class EventGridData
    {
        public static CloudEvent getInstrumentAssignedEvent()
        {
            var instrument = new Instrument
            {
                InstrumentReference = new InstrumentReference
                {
                    InstrumentGroupGuid = "12ed4794-fda9-4187-af34-6da2774b4d28",
                    InstrumentIdentifier = new InstrumentIdentifier
                    {
                        FusionId = "HL001_00119_210329092222",
                        Guid = "787a3292-70bd-41be-bb05-114e47baecc6"
                    },
                    SerialNumber = "210329092222",
                    InstrumentTypeGuid = "4e34e306-84a8-4938-97c4-24e8171b61bc",
                    InstrumentManifestVersionString = "V1.0",
                },
                TenantId = "5ce032aa-3ce9-4fe3-8f57-5c0ed6710a32",
                EdgeInstrumentReference = new InstrumentReference
                {
                    InstrumentGroupGuid = "12ed4794-fda9-4187-af34-6da2774b4d28",
                    InstrumentIdentifier = new InstrumentIdentifier
                    {
                        FusionId = "HL001_00119_FAKE01111111",
                        Guid = "887a3292-70bd-41be-bb05-114e47baecc6"
                    },
                    SerialNumber = "310329092222",
                    InstrumentTypeGuid = "5e34e306-84a8-4938-97c4-24e8171b61bc",
                    InstrumentManifestVersionString = "V1.0",
                },
                ConnectionStatus = EnumConnectionStatus.ConnectionStatusConnected,
                ConnectionStatusReason = EnumConnectionStatusReason.ConnectionStatusReasonHeartbeat,
                ConnectionStatusChangedOn = new ClarosDateTime
                {
                    JsonDateTime = "2025-10-10T10:00:00Z",
                },
                RegistryStatus = EnumInstrumentRegistryStatus.InstrumentRegistryStatusAssigned,
                RecordAuditInfo = new RecordAuditInfo
                {
                    CreatedById = "system",
                    CreatedOn = new ClarosDateTime
                    {
                        JsonDateTime = "2025-10-10T10:00:00Z",
                    },
                    ModifiedById = "system",
                    ModifiedOn = new ClarosDateTime
                    {
                        JsonDateTime = "2025-10-10T10:00:00Z",
                    },
                }
            };

            string instrumentJson = JsonFormatter.Default.Format(instrument);

            var instrumentBinaryData = BinaryData.FromString(instrumentJson);
            var cloudEvent = new CloudEvent(
                source: "Claros.IoT.Registry",
                type: "Instrument.Assigned",
                data: instrumentBinaryData,
                dataContentType: "application/json"
            )
            {
                Id = Guid.NewGuid().ToString(),
                Subject = $"tenant/{instrument.TenantId}/operation/{Guid.NewGuid()}/location/{Guid.NewGuid()}/instrument/{instrument.InstrumentReference.InstrumentIdentifier.Guid}",
                Time = DateTimeOffset.UtcNow
            };

            return cloudEvent;
        }

        public static CloudEvent GetInstrumentManifestEvent()
        {
            var id = Guid.NewGuid().ToString();
            var subTypeId = Guid.Parse("73446485-5e83-4621-974f-d40a7dec601f").ToString();
            var typeId = Guid.Parse("55892762-DA61-4F88-99D1-DC6C93B0F7B3").ToString();
            var manifest = new InstrumentManifest
            {
                Id = id,
                InstrumentType = new InstrumentType
                {
                    Identifier = new InstrumentTypeIdentifier { Id = subTypeId },
                    I18NKeyTextReference = new I18NKeyTextReference { I18NKey = "TestInstrumentSubType" },
                    InstrumentGroupId = typeId
                },
                RecordAuditInfo = new RecordAuditInfo
                {
                    CreatedById = "system",
                    CreatedOn = new ClarosDateTime
                    {
                        JsonDateTime = "2025-10-10T10:00:00Z",
                    },
                    ModifiedById = "system",
                    ModifiedOn = new ClarosDateTime
                    {
                        JsonDateTime = "2025-10-10T10:00:00Z",
                    },
                },
                InstrumentErrorCapability = new InstrumentErrorCapability
                {
                    Enabled = true,
                },
                InstrumentMeasurementCapability = new InstrumentMeasurementCapability
                {
                    Definitions = new InstrumentParameterDefinitions
                    {
                        Items =
                    {
                        new InstrumentParameterDefinition
                        {
                            ParameterId= "ce976ad0-1bd1-4a1f-9ba6-0130fba53c7e",
                            Attributes = new InstrumentParameterDefinitionAttributes
                            {
                                Visible= true,
                                DisplayDecimalPoints = 2,
                                DisplayPriority = EnumInstrumentDataAttributePriority.InstrumentDataAttributePriorityPrimary,
                                DisplaySortOrder = 1,
                            },
                        },
                        new InstrumentParameterDefinition
                        {
                            ParameterId= "84b8bd7f-2420-4e16-855f-7278c5fcf990",
                            Attributes = new InstrumentParameterDefinitionAttributes
                            {
                                Visible = true,
                                DisplayDecimalPoints = 2,
                                DisplayPriority = EnumInstrumentDataAttributePriority.InstrumentDataAttributePrioritySecondary,
                                DisplaySortOrder = 2,
                            },
                        }
                    }
                    }

                },
            };

            string instrumentJson = JsonFormatter.Default.Format(manifest);

            var instrumentBinaryData = BinaryData.FromString(instrumentJson);

            var cloudEvent = new Azure.Messaging.CloudEvent(
                 source: "Claros.IoT.Registry",
                 type: "InstrumentManifest.Created",
                 data: instrumentBinaryData,
                 dataContentType: "application/json"
            )
            {
                Id = id,
                Subject = $"InstrumentManifest/Created/InstrumentType/{subTypeId}",
                Time = DateTimeOffset.UtcNow
            };
            return cloudEvent;
        }

        public static CloudEvent UpdateInstrumentManifest()
        {
            var id = Guid.NewGuid().ToString();
            var subTypeId = Guid.Parse("73446485-5e83-4621-974f-d40a7dec601f").ToString();
            var typeId = Guid.Parse("55892762-da61-4f88-99d1-dc6c93b0f7b3").ToString();
            var manifest = new InstrumentManifest
            {
                Id = id,
                InstrumentType = new InstrumentType
                {
                    Identifier = new InstrumentTypeIdentifier { Id = subTypeId },
                    I18NKeyTextReference = new I18NKeyTextReference { I18NKey = "UpdatedInstrumentSubType" },
                    InstrumentGroupId = typeId

                },
                InstrumentGroup = new InstrumentGroup
                {
                    Id = typeId,
                },
                InstrumentMeasurementCapability = new InstrumentMeasurementCapability
                {
                    Definitions = new InstrumentParameterDefinitions
                    {
                        Items =
                    {
                        new InstrumentParameterDefinition
                        {
                            ParameterId= "ce976ad0-1bd1-4a1f-9ba6-0130fba53c7e",
                            Attributes = new InstrumentParameterDefinitionAttributes
                            {
                                Visible= true,
                                DisplayDecimalPoints = 2,
                                DisplayPriority = EnumInstrumentDataAttributePriority.InstrumentDataAttributePriorityPrimary,
                                DisplaySortOrder = 1,
                            },
                        },
                        new InstrumentParameterDefinition
                        {
                            ParameterId= "84b8bd7f-2420-4e16-855f-7278c5fcf990",
                            Attributes = new InstrumentParameterDefinitionAttributes
                            {
                                Visible = true,
                                DisplayDecimalPoints = 2,
                                DisplayPriority = EnumInstrumentDataAttributePriority.InstrumentDataAttributePrioritySecondary,
                                DisplaySortOrder = 2,
                            },
                        }
                    }
                    }

                },
                RecordAuditInfo = new RecordAuditInfo
                {
                    CreatedById = Guid.NewGuid().ToString(),
                    CreatedOn = new ClarosDateTime
                    {
                        JsonDateTime = "2025-11-06T14:13:35.4700000Z",
                    },
                    ModifiedById = Guid.NewGuid().ToString(),
                    ModifiedOn = new ClarosDateTime
                    {
                        JsonDateTime = "2025-11-06T14:13:35.4700000Z",
                    },
                }
            };

            string instrumentJson = JsonFormatter.Default.Format(manifest);

            var instrumentBinaryData = BinaryData.FromString(instrumentJson);

            var cloudEvent = new Azure.Messaging.CloudEvent(
                 source: "Claros.IoT.Registry",
                 type: "InstrumentManifest.Updated",
                 data: instrumentBinaryData,
                 dataContentType: "application/json"
            )
            {
                Id = id,
                Subject = $"InstrumentManifest/Updated/InstrumentType/{subTypeId}",
                Time = DateTimeOffset.UtcNow
            };
            return cloudEvent;
        }
    }
}
