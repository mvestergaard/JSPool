﻿/*
 * Copyright (c) 2014 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the BSD-style license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using JavaScriptEngineSwitcher.Core;
using JSPool.Exceptions;
using Moq;
using NUnit.Framework;

namespace JSPool.Tests
{
	[TestFixture]
	public class JsPoolTests
	{
		[Test]
		public void ConstructorCreatesEngines()
		{
			var factory = new Mock<IEngineFactoryForMock>();
			var config = new JsPoolConfig
			{
				StartEngines = 5,
				EngineFactory = factory.Object.EngineFactory
			};

			var pool = new JsPool(config);
			Assert.AreEqual(5, pool.AvailableEngineCount);
			factory.Verify(x => x.EngineFactory(), Times.Exactly(5));
		}

		[Test]
		public void GetEngineReturnsAllAvailableEngines()
		{
			var engines = new[]
			{
				new Mock<IJsEngine>().Object,
				new Mock<IJsEngine>().Object,
				new Mock<IJsEngine>().Object,
			};
			var factory = new Mock<IEngineFactoryForMock>();
			factory.SetupSequence(x => x.EngineFactory())
				.Returns(engines[0])
				.Returns(engines[1])
				.Returns(engines[2]);
			var config = new JsPoolConfig
			{
				StartEngines = 3,
				EngineFactory = factory.Object.EngineFactory
			};

			var pool = new JsPool(config);
			var resultEngines = new[]
			{
				pool.GetEngine(),
				pool.GetEngine(),
				pool.GetEngine(),
			};

			CollectionAssert.AreEquivalent(engines, resultEngines);
		}

		[Test]
		public void GetEngineCreatesNewEngineIfNotAtMaximum()
		{
			var factory = new Mock<IEngineFactoryForMock>();
			var config = new JsPoolConfig
			{
				StartEngines = 1,
				MaxEngines = 2,
				EngineFactory = factory.Object.EngineFactory
			};

			var pool = new JsPool(config);
			factory.Verify(x => x.EngineFactory(), Times.Exactly(1));
			pool.GetEngine(); // First engine created on init
			factory.Verify(x => x.EngineFactory(), Times.Exactly(1));
			Assert.AreEqual(1, pool.EngineCount);
			Assert.AreEqual(0, pool.AvailableEngineCount);

			pool.GetEngine(); // Second engine created JIT
			factory.Verify(x => x.EngineFactory(), Times.Exactly(2));
			Assert.AreEqual(2, pool.EngineCount);
			Assert.AreEqual(0, pool.AvailableEngineCount);
		}

		[Test]
		public void GetEngineFailsIfAtMaximum()
		{
			var factory = new Mock<IEngineFactoryForMock>();
			var config = new JsPoolConfig
			{
				StartEngines = 1,
				MaxEngines = 1,
				EngineFactory = factory.Object.EngineFactory
			};

			var pool = new JsPool(config);
			factory.Verify(x => x.EngineFactory(), Times.Exactly(1));
			pool.GetEngine(); // First engine created on init

			Assert.Throws<JsPoolExhaustedException>(() => 
				pool.GetEngine(TimeSpan.Zero)
			);
		}

		[Test]
		public void ReturnEngineToPoolAddsToAvailableEngines()
		{
			var factory = new Mock<IEngineFactoryForMock>();
			var config = new JsPoolConfig
			{
				StartEngines = 2,
				EngineFactory = factory.Object.EngineFactory
			};

			var pool = new JsPool(config);
			Assert.AreEqual(2, pool.AvailableEngineCount);
			var engine = pool.GetEngine();
			Assert.AreEqual(1, pool.AvailableEngineCount);
			pool.ReturnEngineToPool(engine);
			Assert.AreEqual(2, pool.AvailableEngineCount);
		}

		[Test]
		public void DisposeDisposesAllEngines()
		{
			var engines = new[]
			{
				new Mock<IJsEngine>(),
				new Mock<IJsEngine>(),
				new Mock<IJsEngine>(),
			};
			var factory = new Mock<IEngineFactoryForMock>();
			factory.SetupSequence(x => x.EngineFactory())
				.Returns(engines[0].Object)
				.Returns(engines[1].Object)
				.Returns(engines[2].Object);
			var config = new JsPoolConfig
			{
				StartEngines = 3,
				EngineFactory = factory.Object.EngineFactory
			};

			var pool = new JsPool(config);
			pool.Dispose();

			foreach (var engine in engines)
			{
				engine.Verify(x => x.Dispose());
			}
		}
	}

	public interface IEngineFactoryForMock
	{
		IJsEngine EngineFactory();
	}
}