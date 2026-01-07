using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventGrid.Namespaces;
using Google.Protobuf;
using ONE.Models.CSharp.External;
using ONE.Models.CSharp;
using ONE.Models.CSharp.Instrument;
using CloudEvent = Azure.Messaging.CloudEvent;
using Instrument = ONE.Models.CSharp.Instrument.Instrument;
using static Google.Protobuf.Reflection.SourceCodeInfo.Types;
using System.Diagnostics.Metrics;

// Publish a batch of CloudEvents.
SenderClient test = new SenderClient();


var resu = await test.SendEventWithServicePrincipleAsync();
Console.WriteLine(resu);

Console.WriteLine("event have been published to the topic. Press any key to end the application.");
Console.ReadKey();



public class SenderClient
{
    public async Task<bool> SendEventWithServicePrincipleAsync()
    {
        Response response = null;
        //Test
        //var spCredential = new ClientSecretCredential("2c518df7-6644-41f8-8350-3f75e61362ac", "68abd4d5-3ee7-4bde-8886-56e114c909cd", "abc");
        var spCredential = new ClientSecretCredential("2c518df7-6644-41f8-8350-3f75e61362ac", "587b105c-704f-450b-b3ce-16489aae1d67", "abc");

        var client = CreateEventGridSenderClient(spCredential);

        //Update this method call According to requirement
        response = await client.SendAsync(UpdateInstrumentManifest());
        Console.WriteLine($"Response: {response.Status}");

        return IsSuccessResponse(response);
    }

    private EventGridSenderClient CreateEventGridSenderClient(TokenCredential credential)
    {
        var topicEndpoint = new Uri("https://evgn-ihealth-integration-eastus-001.eastus-1.eventgrid.azure.net");

        // Create EventGridSenderClient with full endpoint
        return new EventGridSenderClient(topicEndpoint, "evgt-ihealth-iotregistry", credential);
    }

    private bool IsSuccessResponse(Response response)
    {
        if (response.Status >= 200 && response.Status <= 299)
            return true;
        return false;
    }

    public CloudEvent getInstrumentAssignedEvent()
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
            EdgeInstrumentReference = null,
            ConnectionStatus = EnumConnectionStatus.ConnectionStatusConnected,
            RegistryStatus = EnumInstrumentRegistryStatus.InstrumentRegistryStatusAssigned,
        };

        string instrumentJson = JsonFormatter.Default.Format(instrument);

        var instrumentBinaryData = BinaryData.FromString(instrumentJson);
        var cloudEvent1 = new CloudEvent(
            source: "Claros.IoT.Registry",
            type: "Instrument.Assigned",
            data: instrumentBinaryData,
            dataContentType: "application/json"
        )
        {
            Id = Guid.NewGuid().ToString(),
            Subject = $"tenant/{instrument.TenantId}/instrument/{instrument.InstrumentReference.InstrumentIdentifier.Guid}/operation//location/",
            Time = DateTimeOffset.UtcNow
        };

        return cloudEvent1;

    }

    public CloudEvent GetInstrumentManifestEvent()
    {
        var id = Guid.NewGuid().ToString();
        var subTypeId = Guid.Parse("73446485-5e83-4621-974f-d40a7dec601f").ToString();
        var typeId = Guid.Parse("55892762-DA61-4F88-99D1-DC6C93B0F7B3").ToString();
        var manifest = new ONE.Models.CSharp.Instrument.InstrumentManifest
        {
            Id = id,
            InstrumentType = new InstrumentType
            {
                Identifier = new InstrumentTypeIdentifier { Id = subTypeId },
                I18NKeyTextReference = new ClarosI18NKeyTextReference { I18NKey = "TestInstrumentSubType" },
                InstrumentGroupId = typeId
            },          
            RecordAuditInfo = new RecordAuditInfo()
        };

        string instrumentJson = JsonFormatter.Default.Format(manifest);

        var instrumentBinaryData = BinaryData.FromString(instrumentJson);

        var cloudEvent1 = new Azure.Messaging.CloudEvent(

             source: "Claros.IoT.Registry",

             type: "InstrumentManifest.Created",

             data: instrumentBinaryData,

             dataContentType: "application/json"

        )
        {
            Id = id,
            //InstrumentManifest/Updated/InstrumentType/{instrumentTypeId}
            Subject = $"InstrumentManifest/Created/InstrumentType/{subTypeId}",

            Time = DateTimeOffset.UtcNow

        };
        return cloudEvent1;

    }

    public CloudEvent UpdateInstrumentManifest()
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
                I18NKeyTextReference = new ClarosI18NKeyTextReference { I18NKey = "UpdatedInstrumentSubType" },
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
                CreatedById =Guid.NewGuid().ToString(),
                CreatedOn = null,
                ModifiedById = Guid.NewGuid().ToString(),
                ModifiedOn = new JsonTicksDateTime
                {
                    JsonDateTime= "2025-11-06T14:13:35.4700000Z",
                },
            }
        };

        string instrumentJson = JsonFormatter.Default.Format(manifest);

        var instrumentBinaryData = BinaryData.FromString(instrumentJson);

        var cloudEvent1 = new Azure.Messaging.CloudEvent(

             source: "Claros.IoT.Registry",
             //InstrumentManifest.Updated
             type: "InstrumentManifest.Updated",

             data: instrumentBinaryData,

             dataContentType: "application/json"

        )
        {

            Id = id,

            Subject = $"InstrumentManifest/Updated/InstrumentType/{subTypeId}",

            Time = DateTimeOffset.UtcNow

        };
        return cloudEvent1;

    }
}
