#region Description
/*
# A Pool manager for Unity3D

Author: Karakonstantioglou Spiros, "SaturnTheIcy" (kronos.ice.dev@gmail.com)
 
This is a simple GameObjects pool manager for [Unity3D](http://www.unity3d.com/),
it's only spawn, despawn GameObjects and create or delete pool.

Usage :
- First you must create your pool with PoolCreator with parametre your 
  prefab(GameObject) and optional parameter a size(int). The pool is 
  automaticly prepare for you 2 copy of the GameObject if you don't 
  provide size(int).

  PoolManager.PoolCreator(prefab);
	or
	PoolManager.PoolCreator(prefab, 10);

- To take a prefab(GameObject) from the pool use Spawn with parameter
  your prefab(GameObject) and optionals parameter are position(Vector3)
  and rotation(Quaternion).
 
	PoolManager.Spawn(prefab);
	or
	PoolManager.Spawn(prefab, Vector3.one);
	or
	PoolManager.Spawn(prefab, Vector3.one, Quaternion.identity);

- To return a obj(GameObject) to the pool use Despawn with parametre
  your object(GameObject)

	PoolManager.Despawn(obj);

- To delete your pool and all GameObject inside the pool use DisposePool
  with parameter your prefab(GameObject).

	PoolManager.DisposePool(prefab);

*/
	#region TODO
	#endregion TODO

	#region IDEAS
	// * Check if move the lock the pool to the wrapper is better solution
	// * Add Spawn Despawn with timer? and able to stop it.
	#endregion IDEAS
#endregion Description

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PoolManager
{
	private const int POOL_SIZE = 2;

	// Our pool keeper we use Dictionary with key GameObject
	// for easier search and value the pool that GameObject 
	// represent
	static private Dictionary<GameObject,Pool> pools;

	/// <summary>
	/// Return a GameObject from the pool.
	/// If Pool not exist is create one.
	/// </summary>
	/// <param name="obj">GameObject</param>
	/// <param name="position">Default Vector3.zero</param>
	/// <param name="rotation">Default Quaternion.identity</param>
	/// <returns></returns>
	static public GameObject Spawn(GameObject obj, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion))
	{
		if(obj == null)
			throw new ArgumentNullException("Spawn GameObject");

		CreatePool(obj); // not sure if is correct ways to do

		return pools[obj].Spawn(position, rotation);
	}

	/// <summary>
	/// Store the GameObject to pool, if pool of the 
	/// same obj exist else destroy the GameObject.
	/// </summary>
	/// <param name="obj"></param>
	static public void Despawn(GameObject obj)
	{
		if(obj == null)
			throw new ArgumentNullException("Despawn GameObject");

		// Get from the PoolObject componet the prefab
		GameObject compPrefab = obj.GetComponent<PoolObject>().prefab;


		if(compPrefab == null)
		{
			// If we don't have a pool of this GameObject, destoyed
			GameObject.Destroy(obj);
			return;
		}
		
		pools[compPrefab].Despawn(obj);
	}

	/// <summary>
	/// Destroy the pool of this GameObject
	/// </summary>
	/// <param name="obj"></param>
	static public void DisposePool(GameObject obj)
	{
		if(obj == null)
			throw new ArgumentNullException("DisposePool GameObject");

		// Delete all GameObjects
		pools[obj].DisposeAll(obj);
		// Remove the pool
		pools.Remove(obj);
	}

	/// <summary>
	/// Create pool base on this GameObject with size
	/// </summary>
	/// <param name="prefab"></param>
	/// <param name="size"></param>
	static public void CreatePool(GameObject prefab, int size = POOL_SIZE, ePoolType type = default(ePoolType))
	{
		if(prefab == null)
			throw new ArgumentNullException("GameObject");

		if(pools == null)
		{
			// Create dictionary of our pools
			pools = new Dictionary<GameObject, Pool>();
		}

		if(pools.ContainsKey(prefab) == false)
		{
			
			// Add entry to dictionary with key the GameObject prefab
			// and create new pool for that GameObject with size and
			// type of pool
			pools.Add(prefab, new Pool(prefab, size, type));
		}
	}

	#region Internal Objects

	private class Pool 
	{
		// If the pool is ready to be disposed aka empty of all GameObjects.
		private bool isDisposed;
		// The GameObject to clone.
		private GameObject prefab;
		// The parent object for easier hierachy manage.
		private Transform poolParent;

		private ePoolType type;
		// A "list" like store used to keep none active GameObject. We use Interface for versatility. 
		private IObjectStore<GameObject> thePool { get; set; }
		// Total cloned of objects this pool manage, we do not know if some of
		// the objects are already destroyed manualy and we dont care. Used for
		// names only.
		private int size;
		// Current size of the pool
		public int Count { get { return thePool.Count; } }

		// Pool constructor
		public Pool(GameObject prefab, int capacity = POOL_SIZE, ePoolType type = default(ePoolType))
		{
			if(capacity <= 0)
				throw new ArgumentOutOfRangeException("size", capacity,
						"Argument 'size' must be greater than zero.");
			if(prefab == null)
				throw new ArgumentNullException("GameObject prefab");

			this.prefab = prefab;
			this.size = 0;
			isDisposed = false;
			this.type = type;

			// Create new obj to add all our GameObjects
			// result, a better Hierarchy control.
			poolParent = new GameObject().transform;
			poolParent.name = "pool_" + prefab.name;

			thePool = CreateItemPool<GameObject>(capacity);
			PopulatePool(capacity);
		}

		/// <summary>
		/// Get a GameObject from the pool or
		/// generate new one if empty
		/// </summary>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		/// <returns>GameObject</returns>
		public GameObject Spawn(Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion))
		{
			GameObject obj;

			if(thePool.Count <= 0)
			{
				// Create new clone of prefab GameObject
				obj = CreateObjectClone(prefab);
			}
			else
			{
				// Get the GameObject from our pool
				// locked the pool so no other request happen simutanusly 
				lock(thePool)
				{
					obj = thePool.Acquire();
				}
			}

			// set position and rotation
			obj.transform.position = position;
			obj.transform.rotation = rotation;

			// Enable the GameObject to call the OnEnable()
			obj.SetActive(true);

			return obj; 
		}

		/// <summary>
		/// Disable and store the GameObject
		/// </summary>
		/// <param name="obj">GameObject</param>
		public void Despawn(GameObject obj)
		{
			obj.SetActive(false);

			// locked the pool so no other request happen simutanusly
			lock(thePool)
			{
				thePool.Store(obj);
			}
		}

		/// <summary>
		/// Remove all GameObjects from the pool
		/// and destroy them.
		/// </summary>
		/// <param name="obj">GameObject</param>
		public void DisposeAll(GameObject obj)
		{
			if(isDisposed == true)
			{
				return;
			}

			if(obj != null)
			{
				// locked the pool so no other request happen simutanusly
				lock(thePool)
				{
					GameObject disposable;
					while(thePool.Count > 0)
					{
						disposable = thePool.Acquire();
						Object.Destroy(disposable);
					}
				}

				isDisposed = true;
			}
		}

		/// <summary>
		/// Generate GameObjects and store them
		/// </summary>
		/// <param name="capacity"></param>
		private void PopulatePool(int capacity = POOL_SIZE)
		{
			GameObject obj;

			for(int i = 0; i < capacity; i++)
			{
				obj = CreateObjectClone(prefab);
				Despawn(obj);
			}
		}
		
		/// <summary>
		/// Generate one GameObject
		/// </summary>
		/// <param name="prefab"></param>
		/// <returns>GameObject</returns>
		private GameObject CreateObjectClone(GameObject prefab)
		{
			GameObject clone;
			// Generate clone of the prefab
			clone = (GameObject)GameObject.Instantiate(prefab);
			// And set paret GameObject for easy manage on hierachy
			clone.transform.SetParent(poolParent);
			// Change the name
			clone.name = prefab.name + "_" + size;
			// Add our custome componets
			clone.AddComponent<PoolObject>().prefab = prefab;
			
			size++;
			return clone;
		}

		/// <summary>
		/// Create new pool
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="capacity"></param>
		/// <returns></returns>
		private IObjectStore<T> CreateItemPool<T>(int capacity)
		{
			if(this.type == ePoolType.Queue)
				return new QueuePool<T>(capacity);

			return new StackPool<T>(capacity);
		}
	}	//End class pool

	#region Unity Component
	private class PoolObject : MonoBehaviour
	{
		// We do not want to expose the pool it self
		// so we use the prefab
		public GameObject prefab;

	}
	#endregion

	#region Collection Wrappers

	interface IObjectStore<T>
	{
		T Acquire();
		void Store(T item);
		int Count { get; }
	}


	//Wrapp C# Stack generic class
	class StackPool<T> : Stack<T>, IObjectStore<T>
	{
		public StackPool(int capacity)
			: base(capacity)
		{
		}
		/// <summary>
		/// Get the first item on pool and returned
		/// </summary>
		/// <returns>GameObject</returns>
		public T Acquire()
		{
			return Pop();
		}

		/// <summary>
		/// Store the object on stack
		/// </summary>
		/// <param name="obj"></param>
		public void Store(T obj)
		{
			Push(obj);
		}
	}

	//Wrapp C# Queue generic class
	class QueuePool<T> : Queue<T>, IObjectStore<T>
	{
		public QueuePool(int capacity)
			: base(capacity)
		{
		}

		/// <summary>
		/// Get the first item on pool and returned
		/// </summary>
		/// <returns>GameObject</returns>
		public T Acquire()
		{
			return Dequeue();
		}

		/// <summary>
		/// Store the object on stack
		/// </summary>
		/// <param name="obj"></param>
		public void Store(T item)
		{
			Enqueue(item);
		}
	}
	#endregion Collection Wrappers

	#endregion Internal Objects
}

#region enumarator
public enum ePoolType
{
	Stack = 0,
	Queue = 1
}
#endregion enumarator
