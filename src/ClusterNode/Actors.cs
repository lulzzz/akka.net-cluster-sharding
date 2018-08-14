﻿using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Shared;

namespace ClusterNode
{
    public class Actors
    {
        public static Actors Instance { get; private set; }
        
        public static Actors Build()
        {
            var hocon = File.ReadAllText("cluster.hocon");

            var config = ConfigurationFactory
                .ParseString(hocon)
                .WithFallback(ClusterSingletonManager.DefaultConfig())
                .WithFallback(ClusterSharding.DefaultConfig());

            Instance = new Actors(config);

            return Instance;
        }

        private readonly ActorSystem _system;

        private Actors(Config config)
        {
            _system = ActorSystem.Create(Constants.SystemName, config);

            var sharding = ClusterSharding.Get(_system);
            var settings = ClusterShardingSettings
                .Create(_system)
                .WithRole(Constants.ClusterNodeRoleName);

            ShardRegion = sharding.Start(
                typeName: Customer.TypeName,
                entityProps: Props.Create<Customer>(),
                settings: settings,
                messageExtractor: new MessageExtractor());
        }
        
        public IActorRef ShardRegion { get; }

        public Task StayAlive()
        {
            return _system.WhenTerminated;
        }

        public Task Shutdown()
        {
            return CoordinatedShutdown.Get(_system).Run();
        }
    }
}
