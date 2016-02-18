 A Pool manager for Unity3D
 ==========================
 Author: Karakonstantioglou Spiros, "SaturnTheIcy" (kronos.ice.dev@gmail.com)
 
 This is a simple GameObjects pool manager for [Unity3D](http://www.unity3d.com/),
 it's only spawn, despawn GameObjects and create or delete pool.

 Usage :
 - First you must create your pool(s) with PoolCreator with parametre your prefab(GameObject)
	and optional parameter a size(int). The pool is automaticly prepare for you 2 copy of the 
	GameObject if you don't provide size(int).
		PoolManager.PoolCreator(prefab);
		or
		PoolManager.PoolCreator(prefab, 10);

 - To take a prefab(GameObject) from the pool use Spawn with parameter your prefab(GameObject)
	and optionals parameter are position(Vector3) and rotation(Quaternion).
		PoolManager.Spawn(prefab);
		or
		PoolManager.Spawn(prefab, Vector3.one);
		or
		PoolManager.Spawn(prefab, Vector3.one, Quaternion.identity);

 - To return a obj(GameObject) to the pool use Despawn with parametre your object(GameObject) 
		PoolManager.Despawn(obj);

 - To delete your pool and all GameObject inside the pool use DisposePool with parameter
	your prefab(GameObject).
		PoolManager.DisposePool(prefab);
