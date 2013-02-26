﻿namespace NServiceBus.Transports.SQLServer.Transport
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Threading;
    using NServiceBus.Serializers.Json;
    using NServiceBus.Unicast.Queuing;

    /// <summary>
    ///     SqlServer implementation of <see cref="ISendMessages" />.
    /// </summary>
    public class SqlServerMessageSender : ISendMessages, IDisposable
    {
        private const string SqlSend =
            @"INSERT INTO [{0}] ([Id],[CorrelationId],[ReplyToAddress],[Recoverable],[Expires],[Headers],[Body]) 
                                    VALUES (@Id,@CorrelationId,@ReplyToAddress,@Recoverable,@Expires,@Headers,@Body)";

        private static readonly JsonMessageSerializer Serializer = new JsonMessageSerializer(null);
        private readonly ThreadLocal<SqlTransaction> currentTransaction = new ThreadLocal<SqlTransaction>();
        private bool disposed;

        public string ConnectionString { get; set; }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Sends the given <paramref name="message" /> to the <paramref name="address" />.
        /// </summary>
        /// <param name="message">
        ///     <see cref="TransportMessage" /> to send.
        /// </param>
        /// <param name="address">
        ///     Destination <see cref="Address" />.
        /// </param>
        public void Send(TransportMessage message, Address address)
        {
            if (currentTransaction.IsValueCreated)
            {
                using (
                    var command = new SqlCommand(string.Format(SqlSend, address.Queue),
                                                 currentTransaction.Value.Connection, currentTransaction.Value)
                        {
                            CommandType = CommandType.Text
                        })
                {
                    ExecuteQuery(message, command);
                }
            }
            else
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(string.Format(SqlSend, address.Queue), connection)
                        {
                            CommandType = CommandType.Text
                        })
                    {
                        ExecuteQuery(message, command);
                    }
                }
            }
        }

        /// <summary>
        ///     Sets the native transaction.
        /// </summary>
        /// <param name="transaction">
        ///     Native <see cref="SqlTransaction" />.
        /// </param>
        public void SetTransaction(SqlTransaction transaction)
        {
            currentTransaction.Value = transaction;
        }

        private static void ExecuteQuery(TransportMessage message, SqlCommand command)
        {
            command.Parameters.Add("Id", SqlDbType.UniqueIdentifier).Value = Guid.Parse(message.Id);
            command.Parameters.Add("CorrelationId", SqlDbType.VarChar).Value =
                GetValue(message.CorrelationId);
            if (message.ReplyToAddress == null) // Sendonly endpoint
            {
                command.Parameters.Add("ReplyToAddress", SqlDbType.VarChar).Value = DBNull.Value;
            }
            else
            {
                command.Parameters.Add("ReplyToAddress", SqlDbType.VarChar).Value =
                    message.ReplyToAddress.ToString();
            }
            command.Parameters.Add("Recoverable", SqlDbType.Bit).Value = message.Recoverable;
            if (message.TimeToBeReceived.Ticks > DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks ||
                message.TimeToBeReceived.Ticks < DateTime.MinValue.Ticks - DateTime.UtcNow.Ticks)
            {
                command.Parameters.Add("Expires", SqlDbType.DateTime).Value = DBNull.Value;
            }
            else
            {
                command.Parameters.Add("Expires", SqlDbType.DateTime).Value =
                    DateTime.UtcNow.Add(message.TimeToBeReceived);
            }
            command.Parameters.Add("Headers", SqlDbType.VarChar).Value =
                Serializer.SerializeObject(message.Headers);
            if (message.Body == null)
            {
                command.Parameters.Add("Body", SqlDbType.VarBinary).Value = DBNull.Value;
            }
            else
            {
                command.Parameters.Add("Body", SqlDbType.VarBinary).Value = message.Body;
            }

            command.ExecuteNonQuery();
        }

        private static object GetValue(object value)
        {
            return value ?? DBNull.Value;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed resources.
                currentTransaction.Dispose();
            }

            disposed = true;
        }

        ~SqlServerMessageSender()
        {
            Dispose(false);
        }
    }
}