using System;
using System.Collections.Concurrent;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace ICSharpCode.SharpZipLib.Core
{
	/// <summary>
	/// Pool for <see cref="Inflater"/> instances as they can be costly due to byte array allocations.
	/// </summary>
	public sealed class InflaterPool
	{
		private readonly ConcurrentQueue<PooledInflater> noHeaderPool = new ConcurrentQueue<PooledInflater>();
		private readonly ConcurrentQueue<PooledInflater> headerPool = new ConcurrentQueue<PooledInflater>();

		/// <summary>
		/// Gets a singleton instance of the InflaterPool class.
		/// </summary>
		public static InflaterPool Instance { get; } = new InflaterPool();

		private InflaterPool()
		{
		}

		/// <summary>
		/// Rents an Inflater instance from the pool.
		/// </summary>
		/// <param name="noHeader">
		/// True if no RFC1950/Zlib header and footer fields are expected in the input data
		///
		/// This is used for GZIPed/Zipped input.
		///
		/// For compatibility with
		/// Sun JDK you should provide one byte of input more than needed in
		/// this case.
		/// </param>
		/// <returns>
		/// An Inflater instance from the pool.
		/// 
		/// If the pool is disabled, a new Inflater is created and returned.
		/// </returns>
		public Inflater Rent(bool noHeader = false)
		{
			if (SharpZipLibOptions.InflaterPoolSize <= 0)
			{
				return new Inflater(noHeader);
			}

			var pool = GetPool(noHeader);

			PooledInflater inf;
			if (pool.TryDequeue(out var inflater))
			{
				inf = inflater;
				inf.Reset();
			}
			else
			{
				inf = new PooledInflater(noHeader);
			}

			return inf;
		}

		/// <summary>
		/// Returns an Inflater instance back to the pool for reuse.
		/// </summary>
		/// <param name="inflater">
		/// The Inflater instance to return.
		/// </param>
		/// <exception cref="ArgumentException">
		/// Thrown if the provided inflater is not a PooledInflater instance
		/// obtained from the Rent method.
		/// </exception>
		public void Return(Inflater inflater)
		{
			if (SharpZipLibOptions.InflaterPoolSize <= 0)
			{
				return;
			}

			if (!(inflater is PooledInflater pooledInflater))
			{
				throw new ArgumentException("Returned inflater was not a pooled one");
			}

			var pool = GetPool(inflater.noHeader);
			if (pool.Count < SharpZipLibOptions.InflaterPoolSize)
			{
				pooledInflater.Reset();
				pool.Enqueue(pooledInflater);
			}
		}

		private ConcurrentQueue<PooledInflater> GetPool(bool noHeader) => noHeader ? noHeaderPool : headerPool;
	}
}
