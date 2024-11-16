using Chronicles.CourierApi.Couriers;
using Chronicles.CourierApi.Shipments;
using Chronicles.Documents;
using Chronicles.EventStore.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Chronicles.CourierApi.Options;

public class ConfigureDocumentOptions
    : IConfigureOptions<DocumentOptions>
{
    public void Configure(
        DocumentOptions options)
    {
        options.UseDatabase("CourierDB");
        options.AddDocumentType<CourierDocument>("couriers");
        options.AddDocumentType<ShipmentDocument>("shipments");

        //JsonSerializerOptionsFactory.Change(options.SerializerOptions);
        //options.SerializerOptions.TypeInfoResolverChain.Add(StateJsonSerializerContext.Default);

        options.UseCosmosEmulator(
            allowAnyServerCertificate: true);
        options.AddInitialization(o => o
               .CreateDatabase()
               .CreateContainer<CourierDocument>(p =>
               {
                   p.Id = "couriers";
                   p.PartitionKeyPath = "/pk";
               })
               .CreateContainer<ShipmentDocument>(p =>
               {
                   p.Id = "shipments";
                   p.PartitionKeyPath = "/pk";
               })
               .CreateEventStore());
    }
}