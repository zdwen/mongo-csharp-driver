using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Operations.Serializers;
using MongoDB.Driver.Core.Sessions;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Operation to execute an aggregation framework command.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class AggregationOperation<TResult> : CommandOperationBase<ICursor<TResult>>, IEnumerable<TResult>
    {
        // private fields
        private int _batchSize;
        private CollectionNamespace _collection;
        private object[] _pipeline;
        private ReadPreference _readPreference;
        private IBsonSerializer _serializer;
        private IBsonSerializationOptions _serializationOptions;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregationOperation{TResult}" /> class.
        /// </summary>
        public AggregationOperation()
        {
            _readPreference = ReadPreference.Primary;
        }

        // public properties
        /// <summary>
        /// Gets or sets the size of the batch.
        /// </summary>
        public int BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = value; }
        }

        /// <summary>
        /// Gets or sets the collection.
        /// </summary>
        public CollectionNamespace Collection
        {
            get { return _collection; }
            set { _collection = value; }
        }

        /// <summary>
        /// Gets or sets the pipeline.
        /// </summary>
        public object[] Pipeline
        {
            get { return _pipeline; }
            set { _pipeline = value; }
        }

        /// <summary>
        /// Gets or sets the read preference.
        /// </summary>
        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
            set { _readPreference = value; }
        }

        /// <summary>
        /// Gets or sets the serializer.
        /// </summary>
        public IBsonSerializer Serializer
        {
            get { return _serializer; }
            set { _serializer = value; }
        }

        /// <summary>
        /// Gets or sets the serialization options.
        /// </summary>
        public IBsonSerializationOptions SerializationOptions
        {
            get { return _serializationOptions; }
            set { _serializationOptions = value; }
        }

        // public methods
        /// <summary>
        /// Executes the specified operation behavior.
        /// </summary>
        /// <returns></returns>
        public override ICursor<TResult> Execute()
        {
            EnsureRequiredProperties();

            var command = new BsonDocument
            {
                { "aggregate", _collection.CollectionName },
                { "pipeline", new BsonArray(_pipeline) }
            };

            // don't dispose of channelProvider. The cursor will do that for us.
            var channelProvider = CreateServerChannelProvider(new ReadPreferenceServerSelector(_readPreference), true);
            
            if (channelProvider.Server.BuildInfo.Version >= new Version(2, 5, 1))
            {
                command["cursor"] = new BsonDocument();
                if (_batchSize > 0)
                {
                    command["cursor"]["batchSize"] = _batchSize;
                }
            }

            var aggregateCommandResultSerializer = new AggregateCommandResultSerializer<TResult>(
                _serializer, 
                _serializationOptions);

            var args = new ExecutionArgs
            {
                Command = command,
                Database = new DatabaseNamespace(_collection.DatabaseName),
                ReadPreference = _readPreference,
                Serializer = aggregateCommandResultSerializer
            };

            var result = Execute<AggregateCommandResult<TResult>>(channelProvider, args);

            return new Cursor<TResult>(
                channelProvider: channelProvider,
                cursorId: result.CursorId,
                collection: _collection,
                limit: 0,
                numberToReturn: _batchSize,
                firstBatch: result.Results,
                serializer: _serializer,
                serializationOptions: _serializationOptions,
                timeout: Timeout,
                cancellationToken: CancellationToken,
                readerSettings: GetServerAdjustedReaderSettings(channelProvider.Server));
        }

        /// <summary>
        /// Gets the enumerator. This implicitly calls Execute.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public IEnumerator<TResult> GetEnumerator()
        {
            return Execute();
        }

        // implicit methods
        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // protected methods
        /// <summary>
        /// Validates the required properties.
        /// </summary>
        protected override void EnsureRequiredProperties()
        {
            base.EnsureRequiredProperties();
            Ensure.IsNotNull("Collection", _collection);
            Ensure.IsNotNull("Pipeline", _pipeline);
            Ensure.IsNotNull("ReadPreference", _readPreference);
            if (_serializer == null)
            {
                _serializer = BsonSerializer.LookupSerializer(typeof(TResult));
                if (_serializationOptions == null)
                {
                    _serializationOptions = _serializer.GetDefaultSerializationOptions();
                }
            }
        }
    }
}