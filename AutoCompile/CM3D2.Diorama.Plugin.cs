using System;
using System.Collections.Generic;
using UnityEngine;
using UnityInjector;
using UnityInjector.Attributes;

namespace CM3D2.Diorama
{
	[PluginFilter("CM3D2x64"), PluginFilter("CM3D2x86"), PluginName("Diorama"), PluginVersion("0.0.0.0")]
	public class Diorama : PluginBase
	{
		public class Config
		{
			public class Collision
			{
				public PrimitiveType Primitive = PrimitiveType.Cube;
				public Vector3 Position = Vector3.zero;
				public Vector3 Rotation = Vector3.zero;
				public Vector3 Scale = new Vector3(1, 1, 1);
				public float Bounciness = 0.5f;
				public float DynamicFriction = 0.5f;
				public float StaticFriction = 0.5f;
			}

			public class Rigidbody
			{
				public float Mass = 1.0f;
				public float Drag = 0.0f;
				public float AngularDrag = 0.0f;
				public bool UseGravity = true;
				public bool IsKinematic = true;
			}

			public class Generator
			{
				public float Timespan = 0.0f;
				public float Lifespan = 0.0f;
				public float SpeedMin = 0.0f;
				public float SpeedMax = 0.0f;
				public Vector3 RotationMin = Vector3.zero;
				public Vector3 RotationMax = Vector3.zero;
				public Config.Object Object = new Config.Object();
			}

			public class Model
			{
				public string Name = string.Empty;
				public string File = string.Empty;
				public Vector3 Position = Vector3.zero;
				public Vector3 Rotation = Vector3.zero;
				public Vector3 Scale = Vector3.zero;
				public string InvisibleBones = string.Empty;
				public List<Config.Collision> Collisions = new List<Config.Collision>();
				public List<Config.Generator> Generators = new List<Config.Generator>();
			}

			public class Object
			{
				public string Model = string.Empty;
				public Vector3 Position = Vector3.zero;
				public Vector3 Rotation = Vector3.zero;
				public Vector3 Scale = new Vector3(1, 1, 1);
				public Vector3 LissajousAmplitude = Vector3.zero;
				public Vector3 LissajousFrequency = Vector3.zero;
				public Vector3 LissajousOffset = Vector3.zero;
				public Config.Rigidbody Rigidbody = new Config.Rigidbody();
				public bool RandomColor = false;
			}

			public class Stage
			{
				public string Name = string.Empty;
				public List<Config.Object> Objects = new List<Config.Object>();
				public List<Config.Collision> Collisions = new List<Config.Collision>();
			}

			public bool Enabled = true;
			public KeyCode KeyReload = KeyCode.R;
			public bool PrintStageName = false;
			public bool PrintBoneName = false;
			public bool DrawCenterPos = false;
			public bool DrawCollision = false;
			public List<Config.Model> Models = new List<Config.Model>();
			public List<Config.Stage> Stages = new List<Config.Stage>();
		}

		public class Cache
		{
			public Mesh mesh = null;
			public Material[] materials = null;
		}

		private static Config m_config = new Config();
		private static GameObject m_rootObject = null;
		private static string m_bgName = string.Empty;
		private static Dictionary<string, Cache> m_caches = new Dictionary<string, Cache>();

		private static Texture2D m_tex_r = new Texture2D(1, 1);
		private static Texture2D m_tex_g = new Texture2D(1, 1);
		private static Texture2D m_tex_b = new Texture2D(1, 1);

		public void Awake()
		{
			UnityEngine.Object.DontDestroyOnLoad(this);

			m_tex_r.SetPixels(new Color[1] { new Color(1.0f, 0.0f, 0.0f, 0.25f) }, 0);
			m_tex_r.Apply(false);
			m_tex_g.SetPixels(new Color[1] { new Color(0.0f, 1.0f, 0.0f, 0.25f) }, 0);
			m_tex_g.Apply(false);
			m_tex_b.SetPixels(new Color[1] { new Color(0.0f, 0.0f, 1.0f, 0.25f) }, 0);
			m_tex_b.Apply(false);

			reload();
		}

		public void OnLevelWasLoaded(int lv)
		{
			reload();
		}

		public void Update()
		{
			var bgname = GameMain.Instance.BgMgr.GetBGName();

			if (Input.GetKeyDown(m_config.KeyReload))
			{
				reload();
			}
			else if (bgname == m_bgName &&
				GameObject.Find("DioramaRoot") != null)
			{
				return;
			}

			m_bgName = bgname;
			var root = GameObject.Find("DioramaRoot");
			if (root != null) UnityEngine.Object.DestroyImmediate(root);
			if (!m_config.Enabled) return;
			if (m_config.PrintStageName) Console.WriteLine(m_bgName);
			m_rootObject = new GameObject("DioramaRoot");

			foreach (var s in m_config.Stages)
			{
				if (!m_bgName.Contains(s.Name)) continue;
				foreach (var o in s.Objects) addGameObject(o);
				foreach (var c in s.Collisions) addCollision(c, m_rootObject, m_tex_g);
				break;
			}
		}

		private void reload()
		{
			m_config = loadXml<Config>(System.IO.Path.Combine(this.DataPath, "Diorama.xml"));
			m_caches.Clear();

			// caching
			foreach (var m in m_config.Models)
			{
				loadMesh(m.Name, m.File, new HashSet<string>(m.InvisibleBones.Split(new char[] { ',' })), true);
			}
		}

		public static GameObject addGameObject(Config.Object o)
		{
			foreach (var m in m_config.Models)
			{
				if (m.Name != o.Model) continue;

				var go = new GameObject(m.Name);

				if (m_config.DrawCenterPos)
				{
					var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					sphere.transform.parent = go.transform;
					sphere.transform.localPosition = Vector3.zero;
					sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
					UnityEngine.Object.DestroyImmediate(sphere.GetComponent<SphereCollider>());
				}

				foreach (var c in m.Collisions)
				{
					addCollision(c, go, m_tex_r);
				}

				var mesh = loadMesh(m.Name, m.File, new HashSet<string>(m.InvisibleBones.Split(new char[] { ',' })), false);
				mesh.transform.parent = go.transform;
				mesh.transform.localPosition = m.Position;
				mesh.transform.localRotation = Quaternion.Euler(m.Rotation);
				mesh.transform.localScale = m.Scale;

				go.transform.parent = m_rootObject.transform;
				go.transform.localPosition = o.Position;
				go.transform.localRotation = Quaternion.Euler(o.Rotation);
				go.transform.localScale = o.Scale;

				if (o.RandomColor)
				{
					var color = new Color(
						UnityEngine.Random.Range(0.0f, 1.0f),
						UnityEngine.Random.Range(0.0f, 1.0f),
						UnityEngine.Random.Range(0.0f, 1.0f),
						1.0f);

					foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
					{
						mr.material.SetColor("_Color", color);
					}
				}

				if (o.LissajousAmplitude.magnitude > 0.0f ||
					o.LissajousFrequency.magnitude > 0.0f ||
					o.LissajousOffset.magnitude > 0.0f)
				{
					var lsg = go.AddComponent<LissajousMover>();
					lsg.initialPos = go.transform.position;
					lsg.amplitude = o.LissajousAmplitude;
					lsg.frequency = o.LissajousFrequency;
					lsg.offset = o.LissajousOffset;
				}
				else
				{
					var rb = go.AddComponent<Rigidbody>();
					rb.mass = o.Rigidbody.Mass;
					rb.drag = o.Rigidbody.Drag;
					rb.angularDrag = o.Rigidbody.AngularDrag;
					rb.useGravity = o.Rigidbody.UseGravity;
					rb.isKinematic = o.Rigidbody.IsKinematic;
				}

				foreach (var g in m.Generators)
				{
					var gn = go.AddComponent<Generator>();
					gn.generator = g;
					gn.timespan = g.Timespan;
				}

				return go;
			}

			Console.WriteLine("Model not found :" + o.Model);
			return null;
		}

		private static void addCollision(Config.Collision c, GameObject go, Texture2D tex)
		{
			var col = GameObject.CreatePrimitive(c.Primitive);
			col.transform.parent = go.transform;
			col.transform.localPosition = c.Position;
			col.transform.localRotation = Quaternion.Euler(c.Rotation);
			col.transform.localScale = c.Scale;

			if (m_config.DrawCollision)
			{
				var mat = col.GetComponent<MeshRenderer>().material;
				mat.shader = Shader.Find("Unlit/Transparent");
				mat.SetTexture("_MainTex", tex);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(col.GetComponent<MeshRenderer>());
			}

			var colmat = new PhysicMaterial();
			colmat.bounciness = c.Bounciness;
			colmat.dynamicFriction = c.DynamicFriction;
			colmat.staticFriction = c.StaticFriction;
			col.GetComponent<Collider>().material = colmat;
		}

		private List<Transform> getChildren(Transform parent)
		{
			List<Transform> ret = new List<Transform>();

			foreach (Transform child in parent)
			{
				ret.Add(child);
				ret.AddRange(getChildren(child));
			}

			return ret;
		}

		private T loadXml<T>(string path)
		{
			var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
			using (var sr = new System.IO.StreamReader(path, new System.Text.UTF8Encoding(true)))
			{
				return (T)serializer.Deserialize(sr);
			}
		}

		private static GameObject loadMesh(string name, string filename, HashSet<string> invisibleBone, bool cacheonly)
		{
			var go = cacheonly ? null : new GameObject(name);
			var meshRenderer = cacheonly ? null : go.AddComponent<MeshRenderer>();
			var meshFilter = cacheonly ? null : go.AddComponent<MeshFilter>();

			if (m_caches.ContainsKey(name))
			{
				if (!cacheonly)
				{
					var cache = m_caches[name];
					meshFilter.sharedMesh = cache.mesh;
					meshRenderer.materials = cache.materials;
				}
				return go;
			}

			byte[] data = null;

			using (AFileBase aFileBase = GameUty.FileOpen(filename))
			{
				data = new byte[aFileBase.GetSize()];
				aFileBase.Read(ref data, aFileBase.GetSize());
			}

			var br = new System.IO.BinaryReader(new System.IO.MemoryStream(data), System.Text.Encoding.UTF8);

			string text = br.ReadString();
			if (text != "CM3D2_MESH") throw new FormatException("Invalid file header");

			int version = br.ReadInt32();
			string modelname = br.ReadString();
			string rootbone = br.ReadString();
			int numofbone = br.ReadInt32();

			var mesh = new Mesh();

			for (int i = 0; i < numofbone; i++)
			{
				br.ReadString();
				br.ReadByte();
			}

			for (int i = 0; i < numofbone; i++)
			{
				br.ReadInt32();
			}

			for (int i = 0; i < numofbone; i++)
			{
				br.ReadSingle();
				br.ReadSingle();
				br.ReadSingle();
				br.ReadSingle();
				br.ReadSingle();
				br.ReadSingle();
				br.ReadSingle();
			}

			int numofvert = br.ReadInt32();
			int numofmesh = br.ReadInt32();
			int numofjoints = br.ReadInt32();

			var jointTable = new List<string>();

			for (int i = 0; i < numofjoints; i++)
			{
				var jnt = br.ReadString();
				if (m_config.PrintBoneName) Console.WriteLine(jnt);
				jointTable.Add(jnt);
			}

			for (int i = 0; i < numofjoints; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					br.ReadSingle();
				}
			}

			var vertices = new Vector3[numofvert];
			var normals = new Vector3[numofvert];
			var texcoords = new Vector2[numofvert];
			var tangents = new Vector4[numofvert];

			for (int i = 0; i < numofvert; i++)
			{
				vertices[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
				normals[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
				texcoords[i] = new Vector2(br.ReadSingle(), br.ReadSingle());
			}

			bool existTangents = br.ReadInt32() > 0;
			if (existTangents)
			{
				for (int i = 0; i < numofvert; i++)
				{
					tangents[i] = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
				}
			}

			HashSet<int> removeIndices = new HashSet<int>();
			for (int i = 0; i < numofvert; i++)
			{
				var bi0 = br.ReadUInt16();
				var bi1 = br.ReadUInt16();
				var bi2 = br.ReadUInt16();
				var bi3 = br.ReadUInt16();
				var bw0 = br.ReadSingle();
				var bw1 = br.ReadSingle();
				var bw2 = br.ReadSingle();
				var bw3 = br.ReadSingle();

				bool invisible = false;
				if (bw0 > 0.0f && invisibleBone.Contains(jointTable[bi0])) invisible = true;
				if (bw1 > 0.0f && invisibleBone.Contains(jointTable[bi1])) invisible = true;
				if (bw2 > 0.0f && invisibleBone.Contains(jointTable[bi2])) invisible = true;
				if (bw3 > 0.0f && invisibleBone.Contains(jointTable[bi3])) invisible = true;

				if (invisible)
				{
					removeIndices.Add(i);
				}
			}

			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.uv = texcoords;
			if (existTangents) mesh.tangents = tangents;

			mesh.subMeshCount = numofmesh;
			for (int i = 0; i < numofmesh; ++i)
			{
				int numofindices = br.ReadInt32();
				int[] indices = new int[numofindices];
				for (int j = 0; j < numofindices / 3; j++)
				{
					var idx0 = (int)br.ReadUInt16();
					var idx1 = (int)br.ReadUInt16();
					var idx2 = (int)br.ReadUInt16();
					if (!removeIndices.Contains(idx0) &&
						!removeIndices.Contains(idx1) &&
						!removeIndices.Contains(idx2))
					{
						indices[j * 3 + 0] = idx0;
						indices[j * 3 + 1] = idx1;
						indices[j * 3 + 2] = idx2;
					}
				}
				mesh.SetTriangles(indices, i);
			}

			int numofmaterials = br.ReadInt32();
			var materials = new Material[numofmaterials];

			for (int i = 0; i < numofmaterials; ++i) materials[i] = ImportCM.ReadMaterial(br, null, null);

			if (!cacheonly)
			{
				meshFilter.sharedMesh = mesh;
				meshRenderer.materials = materials;
			}

			var newcache = new Cache();
			newcache.mesh = mesh;
			newcache.materials = materials;
			m_caches[name] = newcache;

			return go;
		}

		public class LissajousMover : MonoBehaviour
		{
			public Vector3 initialPos = Vector3.zero;
			public Vector3 amplitude = Vector3.zero;
			public Vector3 frequency = Vector3.zero;
			public Vector3 offset = Vector3.zero;

			void Update()
			{
				Vector3 prevPos = transform.position;
				transform.position = new Vector3(
					initialPos.x + Mathf.Sin(2.0f * Mathf.PI * frequency.x * Time.time + Mathf.Deg2Rad * offset.x) * amplitude.x,
					initialPos.y + Mathf.Sin(2.0f * Mathf.PI * frequency.y * Time.time + Mathf.Deg2Rad * offset.y) * amplitude.y,
					initialPos.z + Mathf.Sin(2.0f * Mathf.PI * frequency.z * Time.time + Mathf.Deg2Rad * offset.z) * amplitude.z);

				Vector3 dir = transform.position - prevPos;
				if (dir.magnitude > 0) transform.LookAt(transform.position + dir);
			}
		}

		public class Generator : MonoBehaviour
		{
			public Config.Generator generator = null;
			public float timespan = 0.0f;

			void Update()
			{
				timespan -= Time.deltaTime;
				if (timespan < 0.0f)
				{
					var go = Diorama.addGameObject(generator.Object);
					var parent = go.transform.parent;
					go.transform.parent = this.gameObject.transform;
					go.transform.localPosition = generator.Object.Position;
					go.transform.localRotation = Quaternion.Euler(generator.Object.Rotation);
					go.transform.localScale = generator.Object.Scale;
					go.transform.parent = parent;
					timespan = generator.Timespan;

					var rb = go.GetComponent<Rigidbody>();
					if (rb != null)
					{
						var rot = Quaternion.Euler(
						UnityEngine.Random.Range(generator.RotationMin.x, generator.RotationMax.x),
						UnityEngine.Random.Range(generator.RotationMin.y, generator.RotationMax.y),
						UnityEngine.Random.Range(generator.RotationMin.z, generator.RotationMax.z));
						var dir = (rot * go.transform.forward).normalized;
						rb.velocity = dir * UnityEngine.Random.Range(generator.SpeedMin, generator.SpeedMax);
					}

					var lt = go.AddComponent<Lifetimer>();
					lt.lifespan = generator.Lifespan;
				}
			}
		}

		public class Lifetimer : MonoBehaviour
		{
			public float lifespan = 0.0f;

			void Update()
			{
				lifespan -= Time.deltaTime;
				if (lifespan < 0.0f)
				{
					UnityEngine.Object.Destroy(this.gameObject);
				}
			}
		}
	}
}