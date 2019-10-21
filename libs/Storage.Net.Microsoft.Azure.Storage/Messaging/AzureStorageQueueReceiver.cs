﻿using System;
using Storage.Net.Messaging;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Storage.Net.Microsoft.Azure.Storage.Messaging
{
   /// <summary>
   /// Queue receiver based on Azure Storage Queues
   /// </summary>
   class AzureStorageQueueReceiver : PollingMessageReceiver
   {
      private readonly CloudQueueClient _client;
      private readonly string _queueName;
      private readonly CloudQueue _queue;
      private CloudQueue _deadLetterQueue;
      private readonly TimeSpan _messageVisibilityTimeout;
      private readonly TimeSpan _messagePumpPollingTimeout;

      /// <summary>
      /// Creates an instance of Azure Storage Queue receiver 
      /// </summary>
      /// <param name="accountName">Azure Storage account name</param>
      /// <param name="storageKey">Azure Storage key</param>
      /// <param name="queueName">Queue name</param>
      /// <param name="messageVisibilityTimeout">
      /// Timeout value passed in GetMessage call in the Storage Queue. The value indicates how long the message
      /// will be hidden before it reappears in the queue. Therefore you must call <see cref="ConfirmMessagesAsync(IReadOnlyCollection{QueueMessage}, CancellationToken)"/>
      /// to complete and delete it.
      /// </param>
      public AzureStorageQueueReceiver(string accountName, string storageKey, string queueName,
         TimeSpan messageVisibilityTimeout) :
         this(accountName, storageKey, queueName, messageVisibilityTimeout, TimeSpan.FromMinutes(1))
      {
      }

      /// <summary>
      /// Creates an instance of Azure Storage Queue receiver 
      /// </summary>
      /// <param name="accountName">Azure Storage account name</param>
      /// <param name="storageKey">Azure Storage key</param>
      /// <param name="queueName">Queue name</param>
      /// <param name="messageVisibilityTimeout">
      /// Timeout value passed in GetMessage call in the Storage Queue. The value indicates how long the message
      /// will be hidden before it reappears in the queue. Therefore you must call <see cref="ConfirmMessagesAsync(IReadOnlyCollection{QueueMessage}, CancellationToken)"/>
      /// to complete and delete it.
      /// </param>
      /// <param name="messagePumpPollingTimeout">
      /// Indicates how often message pump will ping for new messages in the queue.
      /// </param>
      public AzureStorageQueueReceiver(string accountName, string storageKey, string queueName,
         TimeSpan messageVisibilityTimeout, TimeSpan messagePumpPollingTimeout)
      {
         var account = new CloudStorageAccount(new StorageCredentials(accountName, storageKey), true);
         _client = account.CreateCloudQueueClient();
         _queueName = queueName;
         _queue = _client.GetQueueReference(queueName);
         _queue.CreateIfNotExistsAsync().Wait();
         _messageVisibilityTimeout = messageVisibilityTimeout;
         _messagePumpPollingTimeout = messagePumpPollingTimeout;
      }

      /// <summary>
      /// Returns an approximate message count for this queue
      /// </summary>
      /// <returns></returns>
      public override async Task<int> GetMessageCountAsync()
      {
         await _queue.FetchAttributesAsync();

         return _queue.ApproximateMessageCount ?? 0;
      }

      private async Task<CloudQueue> GetDeadLetterQueue()
      {
         if (_deadLetterQueue == null)
         {
            _deadLetterQueue = _client.GetQueueReference(_queueName + "-deadletter");
            await _deadLetterQueue.CreateIfNotExistsAsync();
         }

         return _deadLetterQueue;
      }

      /// <summary>
      /// Deletes the message from the queue
      /// </summary>
      /// <param name="messages"></param>
      /// <param name="cancellationToken"></param>
      public override async Task ConfirmMessagesAsync(IReadOnlyCollection<QueueMessage> messages, CancellationToken cancellationToken)
      {
         await Task.WhenAll(messages.Select(m => ConfirmAsync(m)));
      }

      private async Task ConfirmAsync(QueueMessage message)
      {
         Converter.SplitId(message.Id, out string id, out string popReceipt);
         if (popReceipt == null)
            throw new ArgumentException("cannot delete message by short id", id);

         await _queue.DeleteMessageAsync(id, popReceipt);
      }

      /// <summary>
      /// Moves message to a dead letter queue which has the same name as original queue prefixed with "-deadletter". This is done because 
      /// Azure Storage queues do not support deadlettering directly.
      /// </summary>
      public override async Task DeadLetterAsync(QueueMessage message, string reason, string errorDescription, CancellationToken cancellationToken)
      {
         var dead = (QueueMessage)message.Clone();
         dead.Properties["deadLetterReason"] = reason;
         dead.Properties["deadLetterError"] = errorDescription;

         CloudQueue deadLetterQueue = await GetDeadLetterQueue();

         await deadLetterQueue.AddMessageAsync(Converter.ToCloudQueueMessage(message));

         await ConfirmMessagesAsync(new[] { message }, cancellationToken);
      }

      /// <summary>
      /// Calls .GetMessages on storage queue
      /// </summary>
      protected override async Task<IReadOnlyCollection<QueueMessage>> ReceiveMessagesAsync(int count, CancellationToken cancellationToken)
      {
         //storage queue can get up to 32 messages
         if (count > 32)
            count = 32;

         IEnumerable<CloudQueueMessage> batch = await _queue.GetMessagesAsync(count, _messageVisibilityTimeout, null, null, cancellationToken);
         if(batch == null) return null;
         List<QueueMessage> result = batch.Select(Converter.ToQueueMessage).ToList();
         return result.Count == 0 ? null : result;
      }
   }
}
