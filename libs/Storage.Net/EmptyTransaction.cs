﻿using System.Threading.Tasks;

namespace Storage.Net
{
   /// <summary>
   /// Transaction object which doesn't actually do anything
   /// </summary>
   public class EmptyTransaction : ITransaction
   {
      private static EmptyTransaction _instance = new EmptyTransaction();

      /// <summary>
      /// Returns empty transaction instance
      /// </summary>
      public static ITransaction Instance => _instance;

      /// <summary>
      /// Doesn't do anything
      /// </summary>
      /// <returns></returns>
      public Task CommitAsync() => Task.FromResult(true);

      /// <summary>
      /// Doesnt do anything
      /// </summary>
      public void Dispose()
      {

      }
   }
}