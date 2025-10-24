using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Reflection;

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/bitcoinblu/bitcoinblu/blob/10a5e93a055ab5f239c5447a5fe05283af09e293/src/chainparams.cpp
	public class BitcoinBlu : NetworkSetBase
	{
		public static BitcoinBlu Instance { get; } = new BitcoinBlu();

		public override string CryptoCode => "BBLU";

		private BitcoinBlu()
		{

		}
			public class BitcoinBluConsensusFactory : ConsensusFactory
	{
		private BitcoinBluConsensusFactory()
		{
		}
		public static BitcoinBluConsensusFactory Instance { get; } = new BitcoinBluConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new BitcoinBluBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new BitcoinBluBlock(new BitcoinBluBlockHeader());
			}
			public override Transaction CreateTransaction()
			{
				return new DogeTransaction();
			}
			public override TxOut CreateTxOut()
			{
				return new DogeTxOut();
			}
			protected override TransactionBuilder CreateTransactionBuilderCore(Network network)
			{
				// https://github.com/bitcoinblu/bitcoinblu/blob/master/doc/fee-recommendation.md
				var txBuilder = base.CreateTransactionBuilderCore(network);
				txBuilder.StandardTransactionPolicy.MinRelayTxFee = new FeeRate(Money.Coins(0.001m), 1000);
				// Around 3000 USD of fee for a transaction at ~0.4 USD per blu
				txBuilder.StandardTransactionPolicy.MaxTxFee = new FeeRate(Money.Coins(56m), 1);
				return txBuilder;
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class AuxPow : IBitcoinSerializable
		{
			Transaction tx = new Transaction();

			public Transaction Transactions
			{
				get
				{
					return tx;
				}
				set
				{
					tx = value;
				}
			}

			uint nIndex = 0;

			public uint Index
			{
				get
				{
					return nIndex;
				}
				set
				{
					nIndex = value;
				}
			}

			uint256 hashBlock = new uint256();

			public uint256 HashBlock
			{
				get
				{
					return hashBlock;
				}
				set
				{
					hashBlock = value;
				}
			}

			List<uint256> vMerkelBranch = new List<uint256>();

			public List<uint256> MerkelBranch
			{
				get
				{
					return vMerkelBranch;
				}
				set
				{
					vMerkelBranch = value;
				}
			}

			List<uint256> vChainMerkleBranch = new List<uint256>();

			public List<uint256> ChainMerkleBranch
			{
				get
				{
					return vChainMerkleBranch;
				}
				set
				{
					vChainMerkleBranch = value;
				}
			}

			uint nChainIndex = 0;

			public uint ChainIndex
			{
				get
				{
					return nChainIndex;
				}
				set
				{
					nChainIndex = value;
				}
			}

			BlockHeader parentBlock = new BlockHeader();

			public BlockHeader ParentBlock
			{
				get
				{
					return parentBlock;
				}
				set
				{
					parentBlock = value;
				}
			}

			public void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWrite(ref tx);
				stream.ReadWrite(ref hashBlock);
				stream.ReadWrite(ref vMerkelBranch);
				stream.ReadWrite(ref nIndex);
				stream.ReadWrite(ref vChainMerkleBranch);
				stream.ReadWrite(ref nChainIndex);
				stream.ReadWrite(ref parentBlock);
			}
		}
		public class DogeTransaction : Transaction
		{
			public override ConsensusFactory GetConsensusFactory()
			{
				return BitcoinBlu.BitcoinBluConsensusFactory.Instance;
			}
		}
		public class DogeTxOut : TxOut
		{
			public override Money GetDustThreshold()
			{
				// https://github.com/bitcoinblu/bitcoinblu/blob/master/doc/fee-recommendation.md
				return Money.Coins(0.01m);
			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return BitcoinBlu.BitcoinBluConsensusFactory.Instance;
			}
		}
		public class BitcoinBluBlock : Block
		{
			public BitcoinBluBlock(BitcoinBluBlockHeader header) : base(header)
			{

			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return BitcoinBluConsensusFactory.Instance;
			}
		}
		public class BitcoinBluBlockHeader : BlockHeader
		{
			const int VERSION_AUXPOW = (1 << 8);

			AuxPow auxPow = new AuxPow();

			public AuxPow AuxPow
			{
				get
				{
					return auxPow;
				}
				set
				{
					auxPow = value;
				}
			}

			public override uint256 GetPoWHash()
			{
				var headerBytes = this.ToBytes();
				var h = NBitcoin.Crypto.SCrypt.ComputeDerivedKey(headerBytes, headerBytes, 1024, 1, 1, null, 32);
				return new uint256(h);
			}

			public override void ReadWrite(BitcoinStream stream)
			{
				base.ReadWrite(stream);
				if((Version & VERSION_AUXPOW) != 0)
				{
					if(!stream.Serializing)
					{
						stream.ReadWrite(ref auxPow);
					}
				}
			}
		}

		public class BitcoinBluTestnetAddressStringParser : NetworkStringParser
		{
			public override bool TryParse(string str, Network network, Type targetType, out IBitcoinString result)
			{
				if (str.StartsWith("tgpv", StringComparison.OrdinalIgnoreCase) && targetType.GetTypeInfo().IsAssignableFrom(typeof(BitcoinExtKey).GetTypeInfo()))
				{
					try
					{
						var decoded = Encoders.Base58Check.DecodeData(str);
						decoded[0] = 0x04;
						decoded[1] = 0x35;
						decoded[2] = 0x83;
						decoded[3] = 0x94;
						result = new BitcoinExtKey(Encoders.Base58Check.EncodeData(decoded), network);
						return true;
					}
					catch
					{
					}
				}
				if (str.StartsWith("tgub", StringComparison.OrdinalIgnoreCase) && targetType.GetTypeInfo().IsAssignableFrom(typeof(BitcoinExtPubKey).GetTypeInfo()))
				{
					try
					{
						var decoded = Encoders.Base58Check.DecodeData(str);
						decoded[0] = 0x04;
						decoded[1] = 0x35;
						decoded[2] = 0x87;
						decoded[3] = 0xCF;
						result = new BitcoinExtPubKey(Encoders.Base58Check.EncodeData(decoded), network);
						return true;
					}
					catch
					{
					}
				}
				return base.TryParse(str, network, targetType, out result);
			}
		}

#pragma warning restore CS0618 // Type or member is obsolete

		//Format visual studio
		//{({.*?}), (.*?)}
		//Tuple.Create(new byte[]$1, $2)
		//static Tuple<byte[], int>[] pnSeed6_main = null;
		//static Tuple<byte[], int>[] pnSeed6_test = null;
		// Not used in DOGE: https://github.com/bitcoinblu/bitcoinblu/blob/10a5e93a055ab5f239c5447a5fe05283af09e293/src/chainparams.cpp#L135

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 100000,
				MajorityEnforceBlockUpgrade = 1500,
				MajorityRejectBlockOutdated = 1900,
				MajorityWindow = 2000,
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(4 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 30,
				//  Not set in reference client, assuming false
				PowNoRetargeting = false,
				//RuleChangeActivationThreshold = 6048,
				//MinerConfirmationWindow = 8064,
				ConsensusFactory = BitcoinBluConsensusFactory.Instance,
				LitecoinWorkCalculation = true,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 25 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 86 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 188 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB3, 0x1F })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAF, 0xE5 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("bb"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("bb"))
			.SetMagic(0xf9beb5da)
			.SetPort(8343)
			.SetRPCPort(8342)
			.SetName("main-blu")
			.AddAlias("mainnet-blu")
			.AddAlias("blu-main")
			.AddAlias("blu-mainnet")
			.AddAlias("bitcoinblu-mainnet")
			.AddAlias("bitcoinblu-main")
			.SetUriScheme("bitcoinblu")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("bitcoinblu.com", "seed.bitcoinblu.com"),
				new DNSSeedData("multiblu.org", "seed.multiblu.org"),
				new DNSSeedData("multiblu.org", "seed.multiblu.org"),
				new DNSSeedData("blur.bitcoinblu.com", "seed.blur.bitcoinblu.com")
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000007a2941fe291883fc9d672928280159ebc098dc46d7fde7a62a5edb87ee95b457ed2c8368ffff001dd2e6ec050101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4a0300000004ffff001d01043e32342f4a756c792f323032352050726f66657373696f6e616c2077726573746c696e672069636f6e2048756c6b20486f67616e2064696573206174203731ffffffff0100f2052a0100000043410400aa9d31f160177ff3ab079d8da42cebf91ecc621ffc4c29583a78bcc6e318e54efacf04cd696ac8049b6da39010f56cb6e8c6813f6a6d407a418dbeb7b2914dac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210000,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000000000000000000"),
				// pre-post-digishield https://github.com/bitcoinblu/bitcoinblu/blob/10a5e93a055ab5f239c5447a5fe05283af09e293/src/chainparams.cpp#L45
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 100,
				//  Not set in reference client, assuming false
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = BitcoinBluConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 113 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 241 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetNetworkStringParser(new BitcoinBluTestnetAddressStringParser())
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tblu"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tblu"))
			.SetMagic(0xdcb7c1fc)
			.SetPort(44556)
			.SetRPCPort(22555)
		   .SetName("blu-test")
		   .AddAlias("blu-testnet")
		   .AddAlias("bitcoinblu-test")
		   .AddAlias("bitcoinblu-testnet")
		   .SetUriScheme("bitcoinblu")
		   .AddDNSSeeds(new[]
		   {
				new DNSSeedData("jrn.me.uk", "testseed.jrn.me.uk")
		   })
		   .AddSeeds(new NetworkAddress[0])
		   .SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000696ad20e2dd4365c7459b4a4a5af743d5e92c6da3229e6532cd605f6533f2a5bb9a7f052f0ff0f1ef7390f000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff1004ffff001d0104084e696e746f6e646fffffffff010058850c020000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 150,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(4 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 60,
				//  Not set in reference client, assuming false
				PowNoRetargeting = false,
				//RuleChangeActivationThreshold = 6048,
				//MinerConfirmationWindow = 8064,
				LitecoinWorkCalculation = true,
				ConsensusFactory = BitcoinBluConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tblu"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tblu"))
			.SetMagic(0xdab5bffa)
			.SetPort(18444)
			.SetRPCPort(44555) // by default this is assigned dynamically, adding port I got for testing
			.SetName("blu-reg")
			.AddAlias("blu-regtest")
			.AddAlias("bitcoinblu-regtest")
			.AddAlias("bitcoinblu-reg")
			.SetUriScheme("bitcoinblu")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000696ad20e2dd4365c7459b4a4a5af743d5e92c6da3229e6532cd605f6533f2a5bdae5494dffff7f20020000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff1004ffff001d0104084e696e746f6e646fffffffff010058850c020000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
			return builder;
		}

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("BitcoinBlu");
		}

	}
}
