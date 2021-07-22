﻿using System;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Crypto;
using Cocona;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using GraphQL.Client.Abstractions;
using System.Threading.Tasks;
using Libplanet.Tx;
using Libplanet.Action;
using Nekoyume.Action;

namespace repeater
{
    public class Program
    {
        private static HashDigest<SHA256> genesisHash = 
            new HashDigest<SHA256>(ByteUtil.ParseHex("4582250d0da33b06779a8475d283d5dd210c683b9b999d74d03fac4f58fa6bce"));
        static void Main(string[] args)
        {
            CoconaLiteApp.Run<Program>(args);
        }

        [Command(Description = "Send empty transaction as ping repeatly.")]
        public async Task Run(
            [Argument]
            string endpoint,
            [Argument]
            string privateKey,
            [Argument]
            int delayMilleSeconds
        )
        {
            using var gqlClient = new GraphQLHttpClient(
                endpoint, 
                new NewtonsoftJsonSerializer()
            );
            var pk = new PrivateKey(ByteUtil.ParseHex(privateKey));

            while (true)
            {
                var nonceRequest = new GraphQLRequest
                {
                    Query = $"{{ nextTxNonce(address:\"{ pk.ToAddress().ToHex()}\") }}",
                };
                var nonce = (await gqlClient.SendQueryAsync<dynamic>(nonceRequest)).Data.nextTxNonce;
                Console.WriteLine(nonce);

                var tx = Transaction<PolymorphicAction<ActionBase>>.Create(
                    (long)nonce,
                    pk,
                    genesisHash,
                    new PolymorphicAction<ActionBase>[] { },
                    null,
                    DateTimeOffset.UtcNow
                );
                var txBase64 = Convert.ToBase64String(tx.Serialize(true));

                var txRequest = new GraphQLRequest
                {
                    Query = $"mutation {{ stageTx(payload: \"{txBase64}\") }}",
                };
                var res = await gqlClient.SendMutationAsync<dynamic>(txRequest);
                Console.WriteLine(res.Data.stageTx);
                
                await Task.Delay(delayMilleSeconds);
            }
        }
    }
}