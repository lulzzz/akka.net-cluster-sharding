﻿using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Configuration;
using Shared;

namespace ApiNode
{
    public class Actors
    {
        public static Actors Instance { get; private set; }

        public static Actors Build()
        {
            var hocon = File.ReadAllText("api.hocon");

            var config = ConfigurationFactory
                .ParseString(hocon)
                .WithFallback(ClusterSharding.DefaultConfig());

            Instance = new Actors(config);

            return Instance;
        }
        
        private readonly ActorSystem _system;

        private Actors(Config config)
        {
            _system = ActorSystem.Create(Constants.SystemName, config);

            var sharding = ClusterSharding.Get(_system);
            
            Proxy = sharding.StartProxy(
                typeName: Customer.TypeName,
                role: Constants.ClusterNodeRoleName,
                messageExtractor: new MessageExtractor());
        }
        
        public IActorRef Proxy { get; }

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
