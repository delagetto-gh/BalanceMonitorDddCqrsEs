﻿using BalanceMonitor.Infrastructure.Core.Interfaces.DDD;
using BalanceMonitor.Infrastructure.Core.Interfaces.EventSourcing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace BalanceMonitor.Infrastructure.Core
{
  public class BalanceMonitorXmlEventStore : IEventStore
  {
    private readonly string xmlFile;

    public BalanceMonitorXmlEventStore()
    {
      this.xmlFile = "BalanceMonitorXmlEventStore.xml";
    }

    public IEnumerable<IDomainEvent> Events
    {
      get
      {
        var domainEvents = new List<IDomainEvent>();
        using (var fs = new FileStream(this.xmlFile, FileMode.OpenOrCreate))
        {
          if (fs.Length > 0)
          {
            var xmlEventsCollectionDeserializer = new DataContractSerializer(typeof(IEnumerable<SerializableXmlEvent>));
            var xmlEventsCollection = (IEnumerable<SerializableXmlEvent>)xmlEventsCollectionDeserializer.ReadObject(fs);
            foreach (var xmlEvent in xmlEventsCollection)
            {
              using (var xmlReader = new XmlTextReader(new StringReader(xmlEvent.PayLoad)))
              {
                var deserializedType = Type.GetType(string.Format("{0}, {1}", xmlEvent.EventType, xmlEvent.AssemblyName)); //Assembly FQN, as events are stored in a separate assembly
                var xmlEventDeserialzer = new DataContractSerializer(deserializedType);
                var @event = xmlEventDeserialzer.ReadObject(xmlReader) as IDomainEvent;
                domainEvents.Add(@event);
              }
            }
          }
          return domainEvents.OrderBy(o => o.DateOccured);
        }
      }
    }

    public void Store<TDomainEvent>(IEnumerable<TDomainEvent> events) where TDomainEvent : IDomainEvent
    {
      var domainEvents = new List<IDomainEvent>(this.Events);
      foreach (var @event in events)
      {
        domainEvents.Add(@event); //append new events to existing list
      }

      var xmlEvents = new List<SerializableXmlEvent>(domainEvents.Count);
      foreach (var @event in domainEvents)
      {
        var xmlEventData = new StringBuilder();
        using (var xmlWriter = XmlWriter.Create(xmlEventData))
        {
          var xmlSerializer = new DataContractSerializer(@event.GetType());
          xmlSerializer.WriteObject(xmlWriter, @event);
        }

        var xmlEvent = new SerializableXmlEvent
        {
          Id = @event.AggregateId,
          PayLoad = xmlEventData.ToString(),
          EventType = @event.GetType().FullName,
          AssemblyName = @event.GetType().Assembly.FullName,
        };
        xmlEvents.Add(xmlEvent);
      }

      //Commit to file
      using (var fs = new FileStream(this.xmlFile, FileMode.Create))
      {
        var xmlEventSerializer = new DataContractSerializer(typeof(IEnumerable<SerializableXmlEvent>));
        xmlEventSerializer.WriteObject(fs, xmlEvents);
      }
    }
  }

  public class SerializableXmlEvent
  {
    public Guid Id { get; set; }

    public string EventType { get; set; }

    public string AssemblyName { get; set; }

    public string PayLoad { get; set; }
  }
}
