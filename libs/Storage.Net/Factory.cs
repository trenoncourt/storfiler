﻿using System;
using System.IO;
using Storage.Net.Blob;
using Storage.Net.Blob.Files;
using Storage.Net.Messaging;
using Storage.Net.ConnectionString;
using Storage.Net.KeyValue;
using Storage.Net.KeyValue.Files;

namespace Storage.Net
{
   /// <summary>
   /// Factory extension methods
   /// </summary>
   public static class Factory
   {
      /// <summary>
      /// Call to initialise a module
      /// </summary>
      public static IModulesFactory Use(this IModulesFactory factory, IExternalModule module)
      {
         if (module == null)
         {
            throw new ArgumentNullException(nameof(module));
         }

         IConnectionFactory connectionFactory = module.ConnectionFactory;
         if (connectionFactory != null)
         {
            ConnectionStringFactory.Register(connectionFactory);
         }

         return factory;
      }

      /// <summary>
      /// Creates a blob stogage instance from a connection string
      /// </summary>
      public static IBlobStorage FromConnectionString(this IBlobStorageFactory factory, string connectionString)
      {
         return ConnectionStringFactory.CreateBlobStorage(connectionString);
      }

      /// <summary>
      /// Creates a key-value storage instance from a connections tring
      /// </summary>
      public static IKeyValueStorage FromConnectionString(this IKeyValueStorageFactory factory, string connectionString)
      {
         return ConnectionStringFactory.CreateKeyValueStorage(connectionString);
      }

      /// <summary>
      /// Creates message publisher
      /// </summary>
      public static IMessagePublisher PublisherFromConnectionString(this IMessagingFactory factory, string connectionString)
      {
         return ConnectionStringFactory.CreateMessagePublisher(connectionString);
      }

      /// <summary>
      /// Creates message receiver
      /// </summary>
      public static IMessageReceiver ReceiverFromConnectionString(this IMessagingFactory factory, string connectionString)
      {
         return ConnectionStringFactory.CreateMessageReceiver(connectionString);
      }

      /// <summary>
      /// Creates a new instance of CSV file storage
      /// </summary>
      /// <param name="factory"></param>
      /// <param name="rootDir"></param>
      public static IKeyValueStorage CsvFiles(this IKeyValueStorageFactory factory,
         DirectoryInfo rootDir)
      {
         return new CsvFileKeyValueStorage(rootDir);
      }

      /// <summary>
      /// Creates an instance in a specific disk directory
      /// <param name="factory"></param>
      /// <param name="directory">Root directory</param>
      /// </summary>
      public static IBlobStorage DirectoryFiles(this IBlobStorageFactory factory,
         DirectoryInfo directory)
      {
         return new DiskDirectoryBlobStorage(directory);
      }

      /// <summary>
      /// Creates an instance of blob storage which stores everyting in memory. Useful for testing purposes only or if blobs don't
      /// take much space.
      /// </summary>
      /// <param name="factory">Factory reference</param>
      /// <returns>In-memory blob storage instance</returns>
      public static IBlobStorage InMemory(this IBlobStorageFactory factory)
      {
         return new InMemoryBlobStorage();
      }

      /// <summary>
      /// Creates a message publisher which holds messages in memory.
      /// </summary>
      /// <param name="factory"></param>
      /// <param name="name">Memory buffer name. Publishers with the same name will contain identical messages. Querying a publisher again
      /// with the same name returns an identical publisher. To create a receiver for this memory bufffer use the same name.</param>
      public static IMessagePublisher InMemoryPublisher(this IMessagingFactory factory, string name)
      {
         return InMemoryMessagePublisherReceiver.CreateOrGet(name);
      }

      /// <summary>
      /// Creates a message receiver to receive messages from a specified memory buffer.
      /// </summary>
      /// <param name="factory"></param>
      /// <param name="name">Memory buffer name. Use the name used when you've created a publisher to receive messages from that buffer.</param>
      public static IMessageReceiver InMemoryReceiver(this IMessagingFactory factory, string name)
      {
         return InMemoryMessagePublisherReceiver.CreateOrGet(name);
      }
   }
}
