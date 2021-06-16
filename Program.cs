using System;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Crypto;
using Cocona;
using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Tx;
using Nekoyume.Action;
using Newtonsoft.Json;

namespace repeater
{
    class Program
    {
        private readonly Currency _ncg;
        private readonly HashDigest<SHA256> _genesisHash;

        public Program()
        {
            _ncg = new Currency("NCG", 2, new Address(ByteUtil.ParseHex("47D082a115c63E7b58B1532d20E631538eaFADde")));
            _genesisHash =
                HashDigest<SHA256>.FromString("4582250d0da33b06779a8475d283d5dd210c683b9b999d74d03fac4f58fa6bce");
        }
        static void Main(string[] args)
        {
            CoconaLiteApp.Run<Program>(args);
        }

        [Command(Description = "Generate and Broadcast Tx")]
        public void SendNcg(
            [Argument("PRIVATE-KEY", Description = "Private Key")] string pkeyStr,
            [Argument("NONCE", Description = "nonce")] int nonce,
            [Argument("RECEIVER-ADDR", Description = "receiver address")] string recvAddrStr,
            [Argument("AMOUNT", Description = "amount")] int amount
            )
        {
            var pkey = new PrivateKey(ByteUtil.ParseHex(pkeyStr));
            var sender = pkey.ToAddress();
            var recv = new Address(ByteUtil.ParseHex(recvAddrStr));
            var action = new TransferAsset(sender, recv, new FungibleAssetValue(_ncg, amount, 0));
            var tx = Transaction<PolymorphicAction<ActionBase>>.Create(
                nonce,
                pkey,
                _genesisHash,
                new PolymorphicAction<ActionBase>[]{/*action*/},
                null,
                DateTimeOffset.UtcNow
            );
            var txSerialized = tx.Serialize(true);
            Console.WriteLine(tx.ToBencodex(true));
            Console.WriteLine(ByteUtil.Hex(txSerialized));
            Console.WriteLine(tx.Id);
        }

        [Command(Description = "Generate and Broadcast Tx")]
        public async void QL()
        {
            var graphQlClient = new GraphQLHttpClient("https://9c-main-full-state.planetarium.dev/graphql", new NewtonsoftJsonSerializer());
            var request = new GraphQLRequest
            {
                Query = @"
{
    nextTxNonce(address:""939e16AD74b499D9607351f3432d63Be6959DE98"")
}
"
            };

            var response =  await graphQlClient.SendQueryAsync<ResponseType>(request);
            
            Console.WriteLine(response.Data.Person.Next.NextTxNonce);
        }
        public class ResponseType 
        {
            public DataType Person { get; set; }
        }

        public class DataType
        {
            public NextTxNonceType Next { get; set; }
        }

        public class NextTxNonceType
        {
            public int NextTxNonce { get; set; }
        }
        

    }
}