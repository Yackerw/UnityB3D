using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class B3DLoader : MonoBehaviour {

	// default materials
	public Material baseMat;
	public Material baseTransMat;
	public Material baseCutMat;

	// if the model is set to be on this layer, it will be fullbright/unlit
	const int skyBoxLayer = 12;


	// size of animation
	int animSize;

    class BChunk
    {
        public string name;
        public int length;
        public int start;
        public BChunk parent;
        public List<BChunk> children;
        public BTexData BTD;
        public BBrusData BBD;
        public BNodeData BND;
        public BVertData BVD;
        public BTriData BTRD;
        public BMeshData BMD;
		public BBoneData BBND;
		public BKeysData BKD;
    }

    class BChunkData
    {
        public int flags;
    }

    class BTexData : BChunkData
    {
        public List<SubTexData> STD;
    }

    class SubTexData
    {
        public string name;
        public int flags;
        public int blend;
        public Vector2 pos;
        public Vector2 scale;
        public float rot;
    }

    class BBrusData : BChunkData
    {
        public int texnum;
        public List<SubBrusData> SBD;
    }

    class SubBrusData
    {
        public string name;
        public float r, g, b, a;
        public float shininess;
        public int blend, fx;
        public int[] texture_id;
    }

    class BNodeData : BChunkData
    {
        public string name;
        public Vector3 pos;
        public Vector3 scale;
        public Quaternion rot;
        public BChunk mesh;
		public BChunk bone;
		public BKeysData anim;
		public BNodeData parent;
		public int id;
		public GameObject obj;
    }

    class BVertData : BChunkData
    {
        public int tex_coord_sets;
        public int tex_coord_set_size;
        public List<SubVertData> SVD;
    }

    class SubVertData
    {
        public Vector3 pos;
        public Vector3 normal;
        public float r, g, b, a;
        public float[][] Tex_Coords;
    }

    class BTriData : BChunkData
    {
        public int brush_id;
        public List<int> tri_ind;
    }

    class BMeshData : BChunkData
    {
        public int brush_id;
        public List<BChunk> verts;
        public List<BChunk> tris;
    }

	class BBoneData : BChunkData
	{
		public List<int> vertex_id;
		public List<float> weight;
	}

	class BKeysData : BChunkData
	{
		public int animFlags;
		public bool[] usedFrameP;
		public bool[] usedFrameR;
		public bool[] usedFrameS;
		public Vector3[] pos;
		public Vector3[] scale;
		public Quaternion[] rot;
	}

    // texture cache
    class TextureRefs
    {
        public Texture2D tex;
        public int refs;
    }

    static Dictionary<string, TextureRefs> textureCache = new Dictionary<string, TextureRefs>();

    List<BBrusData> brushes;
    List<BNodeData> nodes;
    List<BTexData> texs;
    List<Mesh> meshs;
    List<Texture2D> tex2ds;
    public List<Material> matss;

	string GetRelativePath(GameObject o)
	{
		List<GameObject> parents = new List<GameObject>();
		GameObject curr = o;
		while (curr.transform.parent != null)
		{
			parents.Add(curr);
			curr = curr.transform.parent.gameObject;
		}
		string name = "";
		for (int i = parents.Count - 1; i >= 0; --i)
		{
			if (i != parents.Count - 1)
			{
				name = name + "/" + parents[i].name;
			} else
			{
				name = parents[i].name;
			}
		}
		return name;
	}

	Vector3 flip(Vector3 v)
	{
		return new Vector3(v.x, v.z, v.y);
	}

	Quaternion flip(Quaternion q)
	{
		return new Quaternion(q.x, q.z, q.y, q.w);
	}

    BChunk ReadChunk(BChunk parent, FileStream fs)
    {
        byte[] chnk = new byte[4];
        // read name and store it
        fs.Read(chnk, 0, 4);
        BChunk nextChunk = new BChunk();
        // also store start position
        nextChunk.start = (int)fs.Position - 4;
        int i = 0;
        while (i < 4)
        {
            nextChunk.name = String.Concat(nextChunk.name, Convert.ToChar(chnk[i]));
            i++;
        }
        // establish parent/child hierarchy
        nextChunk.parent = parent;
        parent.children.Add(nextChunk);
        nextChunk.children = new List<BChunk>();
        // length, and then return chunk
        fs.Read(chnk, 0, 4);
        nextChunk.length = BitConverter.ToInt32(chnk, 0) + 8;
        ProcessChunk(nextChunk, fs);
        return nextChunk;
    }

    String ReadStringFile(FileStream fs)
    {
        String ret = "";
        byte[] chr = new byte[1];
        fs.Read(chr, 0, 1);
        while (chr[0] != 0)
        {
            ret = String.Concat(ret, Convert.ToChar(chr[0]));
            fs.Read(chr, 0, 1);
        }
        return ret;
    }

    int ReadInt(FileStream fs)
    {
        byte[] ret = new byte[4];
        fs.Read(ret, 0, 4);
        return BitConverter.ToInt32(ret, 0);
    }

    float ReadFloat(FileStream fs)
    {
        byte[] ret = new byte[4];
        fs.Read(ret, 0, 4);
        return BitConverter.ToSingle(ret, 0);
    }

    void ProcessChunk(BChunk chunk, FileStream fs)
    {
        switch (chunk.name)
        {
            case "BB3D":
                while (fs.Position < chunk.length)
                {
                    ReadChunk(chunk, fs);
                }
            break;

            case "TEXS":
                {
                    BTexData BTD = new BTexData();
                    texs.Add(BTD);
                    BTD.STD = new List<SubTexData>();
                    while (fs.Position < chunk.length + chunk.start)
                    {
                        SubTexData STD = new SubTexData();
                        STD.name = ReadStringFile(fs);
                        STD.flags = ReadInt(fs);
                        STD.blend = ReadInt(fs);
                        STD.pos.x = ReadFloat(fs);
                        STD.pos.y = ReadFloat(fs);
                        STD.scale.x = ReadFloat(fs);
                        STD.scale.y = -ReadFloat(fs);
                        STD.rot = ReadFloat(fs);
                        BTD.STD.Add(STD);
                    }
                    chunk.BTD = BTD;
                }
                break;

            case "BRUS":
                {
                    BBrusData BBD = new BBrusData();
                    brushes.Add(BBD);
                    BBD.texnum = ReadInt(fs);
                    BBD.SBD = new List<SubBrusData>();
                    while (fs.Position < chunk.length + chunk.start)
                    {
                        SubBrusData SBD = new SubBrusData();
                        SBD.name = ReadStringFile(fs);
                        SBD.r = ReadFloat(fs);
                        SBD.g = ReadFloat(fs);
                        SBD.b = ReadFloat(fs);
                        SBD.a = ReadFloat(fs);
                        SBD.shininess = ReadFloat(fs);
                        SBD.blend = ReadInt(fs);
                        SBD.fx = ReadInt(fs);
                        SBD.texture_id = new int[BBD.texnum];
                        int i = 0;
                        while (i < BBD.texnum)
                        {
                            SBD.texture_id[i] = ReadInt(fs);
                            i++;
                        }
                        BBD.SBD.Add(SBD);
                    }
                    chunk.BBD = BBD;
                }
                break;
            
            case "NODE":
                {
                    BNodeData BND = new BNodeData();
                    BND.name = ReadStringFile(fs);
                    BND.pos.x = ReadFloat(fs);
                    BND.pos.y = ReadFloat(fs);
                    BND.pos.z = ReadFloat(fs);
                    BND.scale.x = ReadFloat(fs);
                    BND.scale.y = ReadFloat(fs);
                    BND.scale.z = ReadFloat(fs);
					BND.pos = flip(BND.pos);
					BND.scale = flip(BND.scale);
					// it's w x y z
					float w = ReadFloat(fs);
					float x = ReadFloat(fs);
					float y = ReadFloat(fs);
					float z = ReadFloat(fs);
					BND.rot = new Quaternion(x, y, z, w).normalized;
					BND.rot = flip(BND.rot);
                    // relative scale and position
                    if (chunk.parent.name == "NODE")
                    {
						BND.parent = chunk.parent.BND;
                    }
                    chunk.BND = BND;
                    while (fs.Position < chunk.start + chunk.length)
                    {
                        BChunk msh = ReadChunk(chunk, fs);
                        if (msh.name == "MESH")
                        {
                            BND.mesh = msh;
                        }
						if (msh.name == "BONE")
						{
							BND.bone = msh;
						}
						if (msh.name == "KEYS")
						{
							// sorry nothing
							/*if (BND.anim == null)
							{
								BND.anim = msh;
							}*/
						}
                    }
                    // we're done here
                    nodes.Add(BND);
					BND.id = nodes.Count;
                }
                break;

            case "MESH":
                {
                    BMeshData BMD = new BMeshData();
                    BMD.brush_id = ReadInt(fs);
                    //BMD.verts = ReadChunk(chunk, fs);
                    chunk.BVD = new BVertData();
                    BMD.verts = new List<BChunk>();
                    BMD.tris = new List<BChunk>();
                    // potentially multiple TRI chunks
                    while (fs.Position < chunk.start + chunk.length)
                    {
                        BChunk chnk = ReadChunk(chunk, fs);
                        if (chnk.name == "TRIS")
                        {
                            BMD.tris.Add(chnk);
                        }
                        if (chnk.name == "VRTS")
                        {
                            BMD.verts.Add(chnk);
                        }
                    }
                    chunk.BMD = BMD;
                }
                break;

            case "VRTS":
                {
                    BVertData BVD = chunk.parent.BVD; // = new BVertData();
                    BVD.flags = ReadInt(fs);
                    BVD.tex_coord_sets = ReadInt(fs);
                    BVD.tex_coord_set_size = ReadInt(fs);
                    BVD.SVD = new List<SubVertData>();
                    while (fs.Position < chunk.start + chunk.length)
                    {
                        SubVertData SVD = new SubVertData();
                        SVD.pos = flip(new Vector3(ReadFloat(fs), ReadFloat(fs), ReadFloat(fs)));
                        if ((BVD.flags & 1) != 0)
                        {
                            SVD.normal = flip(new Vector3(ReadFloat(fs), ReadFloat(fs), ReadFloat(fs)));
                        } else
                        {
                            SVD.normal = Vector3.zero;
                        }
                        if ((BVD.flags & 2) != 0)
                        {
                            SVD.r = ReadFloat(fs);
                            SVD.g = ReadFloat(fs);
                            SVD.b = ReadFloat(fs);
                            SVD.a = ReadFloat(fs);
                        } else
                        {
                            SVD.r = 1;
                            SVD.g = 1;
                            SVD.b = 1;
                            SVD.a = 1;
                        }
                        SVD.Tex_Coords = new float[BVD.tex_coord_sets][];
                        int i = 0;
                        while (i < BVD.tex_coord_sets)
                        {
                            SVD.Tex_Coords[i] = new float[BVD.tex_coord_set_size];
                            int i2 = 0;
                            while (i2 < BVD.tex_coord_set_size) {
                                SVD.Tex_Coords[i][i2] = ReadFloat(fs);
                                i2++;
                            }
                            i++;
                        }
                        // default UV maps
                        if (BVD.tex_coord_sets == 0 || BVD.tex_coord_set_size == 0)
                        {
                            SVD.Tex_Coords = new float[1][];
                            SVD.Tex_Coords[0] = new float[2];
                            SVD.Tex_Coords[0][0] = 0;
                            SVD.Tex_Coords[0][1] = 0;
                        }
                        BVD.SVD.Add(SVD);
                    }
                    chunk.BVD = BVD;
                }
                break;

                case "TRIS": {
                    BTriData BTD = new BTriData();
                    BTD.brush_id = ReadInt(fs);
                    BTD.tri_ind = new List<int>();
                    while (fs.Position < chunk.start + chunk.length)
                    {
                        BTD.tri_ind.Add(ReadInt(fs));
                        BTD.tri_ind.Add(ReadInt(fs));
                        BTD.tri_ind.Add(ReadInt(fs));
						int tmp = BTD.tri_ind[BTD.tri_ind.Count - 1];
						BTD.tri_ind[BTD.tri_ind.Count - 1] = BTD.tri_ind[BTD.tri_ind.Count - 2];
						BTD.tri_ind[BTD.tri_ind.Count - 2] = tmp;
                    }
                    chunk.BTRD = BTD;
                }
                break;

			case "BONE":
				{
					BBoneData BBD = new BBoneData();
					BBD.vertex_id = new List<int>();
					BBD.weight = new List<float>();
					while (fs.Position < chunk.length + chunk.start)
					{
						BBD.vertex_id.Add(ReadInt(fs));
						BBD.weight.Add(ReadFloat(fs));
					}
					chunk.BBND = BBD;
				}
				break;

			case "KEYS":
				{
					BKeysData BKD;// = new BKeysData();
					if (chunk.parent.BND.anim == null)
					{
						BKD = new BKeysData();
						BKD.pos = new Vector3[0];
						BKD.scale = new Vector3[0];
						BKD.rot = new Quaternion[0];
						BKD.usedFrameP = new bool[0];
						BKD.usedFrameS = new bool[0];
						BKD.usedFrameR = new bool[0];
					} else
					{
						BKD = chunk.parent.BND.anim;
					}
					int lFlag = ReadInt(fs);
					BKD.animFlags |= lFlag;
					// we need to keep track of used frames separately for each anim type, as all KEYS chunks on a NODE are concatenated
					while (fs.Position < chunk.length + chunk.start)
					{
						int frame = ReadInt(fs);
						if ((lFlag & 1) != 0)
						{
							if (BKD.usedFrameP.Length <= frame)
							{
								Array.Resize(ref BKD.usedFrameP, frame + 1);
							}
							BKD.usedFrameP[frame] = true;
							if (BKD.pos.Length <= frame)
							{
								Array.Resize(ref BKD.pos, frame + 1);
							}
							BKD.pos[frame] = flip(new Vector3(ReadFloat(fs), ReadFloat(fs), ReadFloat(fs)));
						}
						if ((lFlag & 2) != 0)
						{
							if (BKD.usedFrameS.Length <= frame)
							{
								Array.Resize(ref BKD.usedFrameS, frame + 1);
							}
							BKD.usedFrameS[frame] = true;
							if (BKD.scale.Length <= frame)
							{
								Array.Resize(ref BKD.scale, frame + 1);
							}
							BKD.scale[frame] = flip(new Vector3(ReadFloat(fs), ReadFloat(fs), ReadFloat(fs)));
						}
						if ((lFlag & 4) != 0)
						{
							if (BKD.usedFrameR.Length <= frame)
							{
								Array.Resize(ref BKD.usedFrameR, frame + 1);
							}
							BKD.usedFrameR[frame] = true;
							if (BKD.rot.Length <= frame)
							{
								Array.Resize(ref BKD.rot, frame + 1);
							}
							float w = ReadFloat(fs);
							BKD.rot[frame] = flip(new Quaternion(ReadFloat(fs), ReadFloat(fs), ReadFloat(fs), w));
						}
					}
					chunk.BKD = BKD;
					chunk.parent.BND.anim = BKD;
				}
				break;

			case "ANIM":
				{
					// so, this isn't correct - anims are actually supposed to be node specific, but that's incredibly dumb, no one ever uses them that way, and that'd just create an unreasonable number of animators
					fs.Seek(4, SeekOrigin.Current);
					animSize = Mathf.Max(animSize, ReadInt(fs));
					fs.Seek(4, SeekOrigin.Current);
				}
				break;

            // bones, keys, and anims are finally used. TODO: support the proprietary SEQS chunk?
            default:
                {
                    fs.Seek(chunk.length + chunk.start, SeekOrigin.Begin);
                }
                break;
        }
    }

    // this nukes everything when ever any b3d is destroyed. since b3ds are usually only destroyed at stage end, though, i don't care.
	// if you want to be able to destroy b3ds without destroying shared resources, just make textureCache non-static
    private void OnDestroy()
    {
		// free memory
		if (meshs != null)
		{
			for (int i = 0; i < meshs.Count; i++)
			{
				Destroy(meshs[i]);
			}
		}
		if (tex2ds != null)
		{
			for (int i = 0; i < tex2ds.Count; i++)
			{
				Destroy(tex2ds[i]);
			}
		}
		if (matss != null)
		{
			for (int i = 0; i < matss.Count; i++)
			{
				Destroy(matss[i]);
			}
		}
        // nuke the cache
        textureCache = new Dictionary<string, TextureRefs>();
    }

    public void LoadB3D(string filename, bool vis, bool col, Vector3 pos, Vector3 scale, Vector3 rot, bool castShadows, int rendLayer = 0)
    {
		FileStream fs = null;
		try
		{
			fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
		} catch
		{
			return;
		}
        BChunk main = new BChunk();
        main.start = 0;
        byte[] chunk = new byte[4];
        fs.Read(chunk, 0, 4);
        int i = 0;
        while (i < 4)
        {
            main.name = String.Concat(main.name, Convert.ToChar(chunk[i]));
            i++;
        }
        // not b3d file, return
        if (main.name != "BB3D")
        {
            fs.Close();
            return;
        }
        main.children = new List<BChunk>();

        brushes = new List<BBrusData>();
        nodes = new List<BNodeData>();
        texs = new List<BTexData>();
        meshs = new List<Mesh>();
        tex2ds = new List<Texture2D>();
        matss = new List<Material>();


        fs.Read(chunk, 0, 4);
        main.length = BitConverter.ToInt32(chunk, 0) + 8;
        fs.Read(chunk, 0, 4);
        // exists, but i'm not gonna do anything with it
        //int BVer = BitConverter.ToInt32(chunk, 0);
        // main loop
        ProcessChunk(main, fs);

        FinishB3DLoad(filename, vis, col, pos, scale, rot, rendLayer, castShadows);
        return;
    }

	Texture2D ApplyMask(Texture2D tex)
	{
		Texture2D retTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, true);
		// if the texture is masked, set the alpha on black pixels
		Color[] c = tex.GetPixels();
		for (int i = 0; i < c.Length; ++i)
		{
			if (c[i].r <= 0.01f && c[i].g <= 0.01f && c[i].b <= 0.01f)
			{
				c[i].a = 0;
			}
		}
		retTex.SetPixels(c);
		retTex.Apply();
		return retTex;
	}

	void FinishB3DLoad(string filename, bool vis, bool col, Vector3 pos, Vector3 scale, Vector3 rot, int rendLayer, bool castShadows)
	{
		// textures
		byte[] fn = System.Text.Encoding.ASCII.GetBytes(filename);
		int i = fn.Length - 1;
		while (fn[i] != 0x2F && fn[i] != 0x5C)
		{
			--i;
		}
		String cutFileName = filename.Substring(0, i);
		Texture2D[] tex = new Texture2D[0];
		if (texs.Count != 0)
		{
			tex = new Texture2D[texs[0].STD.Count];
		}
		i = 0;
		while (texs.Count != 0 && i < texs[0].STD.Count)
		{
			// strip directory from texture...
			byte[] tfn = System.Text.Encoding.ASCII.GetBytes(texs[0].STD[i].name);
			int i2 = tfn.Length - 1;
			int start = tfn.Length;
			while (i2 > 0 && tfn[i2] != 0x2F && tfn[i2] != 0x5C)
			{
				i2--;
			}
			// ensure i2 - and therefor the texture name - are valid
			if (i2 != -1)
			{
				String newTexPath = texs[0].STD[i].name.Substring(i2, start - i2);
				byte extn = tfn[tfn.Length - 3];
				if (tfn[0] != 0x2F && tfn[0] != 0x5C)
				{
					newTexPath = string.Concat("/", newTexPath);
				}
				// get file extension
				if (extn == 0x62 || extn == 0x42)
				{
					// cache textures
					if (!textureCache.ContainsKey(newTexPath))
					{
						textureCache[newTexPath] = new TextureRefs();
						textureCache[newTexPath].tex = BMPLoader.Load(string.Concat(Application.dataPath, "/../", cutFileName, newTexPath));
					}
					tex[i] = textureCache[newTexPath].tex;
					if ((texs[0].STD[i].flags & 4) != 0)
					{
						tex[i] = ApplyMask(tex[i]);
					}
					textureCache[newTexPath].refs++;
				}
				else if (extn == 0x64 || extn == 0x44)
				{
					// cache textures
					if (!textureCache.ContainsKey(newTexPath))
					{
						textureCache[newTexPath] = new TextureRefs();
						textureCache[newTexPath].tex = DDSLoader.Load(string.Concat(Application.dataPath, "/../", cutFileName, newTexPath));
					}
					tex[i] = textureCache[newTexPath].tex;
					if ((texs[0].STD[i].flags & 4) != 0)
					{
						tex[i] = ApplyMask(tex[i]);
					}
					textureCache[newTexPath].refs++;
				}
				else
				{
					// cache textures
					if (!textureCache.ContainsKey(newTexPath))
					{
						textureCache[newTexPath] = new TextureRefs();
						textureCache[newTexPath].tex = new Texture2D(2, 2);
						FileStream img = null;
						try
						{
							img = File.Open(string.Concat(Application.dataPath, "/../", cutFileName, newTexPath), FileMode.Open);
						}
						catch
						{
							img = null;
						}
						if (img != null)
						{
							img.Seek(0, SeekOrigin.End);
							byte[] data = new byte[img.Position];
							img.Seek(0, SeekOrigin.Begin);
							img.Read(data, 0, data.Length);
							img.Close();
							textureCache[newTexPath].tex.LoadImage(data);
						}
					}
					tex[i] = textureCache[newTexPath].tex;
					if ((texs[0].STD[i].flags & 4) != 0)
					{
						tex[i] = ApplyMask(tex[i]);
					}
				}
			}
			i++;
		}

		Material[] mats;
		// brushes
		if (brushes.Count != 0)
		{
			mats = new Material[brushes[0].SBD.Count];
			i = 0;
			while (i < brushes[0].SBD.Count)
			{
				if (rendLayer != skyBoxLayer)
				{
					//mats[i] = new Material(Shader.Find("Standard"));
					mats[i] = new Material(baseMat);
					matss.Add(mats[i]);
				}
				else
				{
					mats[i] = new Material(Shader.Find("Unlit/Texture"));
					matss.Add(mats[i]);
				}
				SubBrusData SBD = brushes[0].SBD[i];
				// fade rendering iirc
				if (brushes[0].SBD[i].a != 1.0f || ((SBD.texture_id.Length != 0 && SBD.texture_id[0] != -1) && ((texs[0].STD[SBD.texture_id[0]].flags & 2) != 0 || (texs[0].STD[SBD.texture_id[0]].flags & 4) != 0)))
				{
					// if it's not masked, use fade. otherwise, use cutout
					if (((SBD.texture_id.Length != 0 && SBD.texture_id[0] != -1) && (texs[0].STD[SBD.texture_id[0]].flags & 4) == 0) || SBD.texture_id.Length == 0)
					{
						/*mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
						mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
						mats[i].SetInt("_ZWrite", 0);
						mats[i].DisableKeyword("_ALPHATEST_ON");
						mats[i].EnableKeyword("_ALPHABLEND_ON");
						mats[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
						mats[i].renderQueue = 3000;*/
						//mats[i].shader = baseTransMat.shader;
						mats[i] = new Material(baseTransMat);
					} else
					{
						/*mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
						mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
						mats[i].SetInt("_ZWrite", 1);
						mats[i].EnableKeyword("_ALPHATEST_ON");
						mats[i].DisableKeyword("_ALPHABLEND_ON");
						mats[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
						mats[i].renderQueue = 2450;*/
						//mats[i].shader = baseCutMat.shader;
						mats[i] = new Material(baseCutMat);
						mats[i].SetFloat("_Cutoff", 0.5f);
					}
				}
				if (SBD.texture_id.Length != 0 && SBD.texture_id[0] != -1)
				{
					int starti2 = 1;
					if ((texs[0].STD[SBD.texture_id[0]].flags & 64) == 0)
					{
						mats[i].mainTexture = tex[SBD.texture_id[0]];
						mats[i].mainTextureScale = texs[0].STD[SBD.texture_id[0]].scale;
						mats[i].mainTextureOffset = texs[0].STD[SBD.texture_id[0]].pos;
					} else
					{
						starti2 = 0;
					}
					/*if (SBD.texture_id.Length > 1 && SBD.texture_id[1] != -1)
					{
						mats[i].EnableKeyword("_DETAIL_MULX2");
						mats[i].SetTexture("_DetailAlbedoMap", tex[SBD.texture_id[1]]);
						mats[i].SetTextureScale("_DetailAlbedoMap", new Vector2(texs[0].STD[SBD.texture_id[0]].scale.x, -texs[0].STD[SBD.texture_id[0]].scale.y));
						mats[i].SetTextureOffset("_DetailAlbedoMap", texs[0].STD[SBD.texture_id[0]].pos);
					}*/
					// get other maps
					for (int i2 = starti2; i2 < SBD.texture_id.Length; ++i2)
					{
						if (SBD.texture_id[i2] != -1)
						{
							int fl = texs[0].STD[SBD.texture_id[i2]].flags;
							int blendType = texs[0].STD[SBD.texture_id[i2]].blend;
							// reject any textures that are ONLY "color"
							if (fl != 1)
							{
								// check for sphere maps and blend type
								if ((fl & 64) != 0)
								{
									switch (blendType)
									{
										case 3:
											{
												mats[i].SetTexture("_AddSphereMap", tex[SBD.texture_id[i2]]);
												mats[i].EnableKeyword("ADDSPHERE_ON");
											}
											break;
										case 2:
											{
												mats[i].SetTexture("_MulSphereMap", tex[SBD.texture_id[i2]]);
												mats[i].EnableKeyword("MULSPHERE_ON");
											}
											break;
									}
								}
								else
								{
									// TODO: check for cubemap lol
									switch (blendType)
									{
										case 3:
											{
												mats[i].SetTexture("_AddTex", tex[SBD.texture_id[i2]]);
												mats[i].EnableKeyword("ADDTEX_ON");
												if (texs[0].STD[SBD.texture_id[i2]].scale != Vector2.one)
												{
													mats[i].SetTextureOffset("_AddTex", texs[0].STD[SBD.texture_id[i2]].pos);
													mats[i].SetTextureScale("_AddTex", texs[0].STD[SBD.texture_id[i2]].scale);
												}
											}
											break;
										case 2:
											{
												mats[i].SetTexture("_MulTex", tex[SBD.texture_id[i2]]);
												mats[i].EnableKeyword("MULTEX_ON");
												if (texs[0].STD[SBD.texture_id[i2]].scale != Vector2.one)
												{
													mats[i].SetTextureOffset("_MulTex", texs[0].STD[SBD.texture_id[i2]].pos);
													mats[i].SetTextureScale("_MulTex", texs[0].STD[SBD.texture_id[i2]].scale);
												}
											}
											break;
										case 5:
											{
												mats[i].SetTexture("_MulTex2", tex[SBD.texture_id[i2]]);
												mats[i].EnableKeyword("MULTEX2_ON");
												if (texs[0].STD[SBD.texture_id[i2]].scale != Vector2.one)
												{
													mats[i].SetTextureOffset("_MulTex2", texs[0].STD[SBD.texture_id[i2]].pos);
													mats[i].SetTextureScale("_MulTex2", texs[0].STD[SBD.texture_id[i2]].scale);
												}
											}
											break;
									}
								}
							}
						}
					}
				}
				else
				{
					mats[i].mainTexture = null;
				}
				mats[i].SetFloat("_Glossiness", SBD.shininess);
				if (SBD.shininess != 0f)
				{
					mats[i].EnableKeyword("SPEC_ON");
				}
				mats[i].SetColor("_Color", new Color(SBD.r, SBD.g, SBD.b, SBD.a));
				i++;
			}
		} else
		{
			// default mat
			mats = new Material[1];
			mats[0] = new Material(Shader.Find("Standard"));
		}

		// done, now make mesh
		GameObject[] objs = new GameObject[nodes.Count];
		i = nodes.Count - 1;
		Vector3[] vertpos;
		Vector2[] uvs;
		Vector2[] uvs2 = new Vector2[1];
		Vector3[] norms;
		// store all the vertex weights and bones in separate lists for each mesh
		BoneWeight[] vertWeights = new BoneWeight[1];
		List<Transform> bones = new List<Transform>();
		List<BoneWeight[]> vWeights = new List<BoneWeight[]>();
		List<Transform[]> bons = new List<Transform[]>();
		List<Matrix4x4> bindPoses = new List<Matrix4x4>();
		List<Matrix4x4[]> binds = new List<Matrix4x4[]>();
		List<BNodeData> boneParents = new List<BNodeData>();
		bool inBone = false;
		GameObject boneParent = new GameObject("Skeleton");
		boneParent.transform.parent = gameObject.transform;
		//boneParent.transform.rotation = Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 180f, 0f);
		//boneParent.transform.localScale = new Vector3(-1f, 1f, 1f);
		bool bonesExist = false;
		// create bones first since those are important to the rest of the mesh creation
		while (i >= 0)
		{
			if (nodes[i].bone == null || nodes[i].bone.name != "BONE")
			{
				--i;
				if (inBone)
				{
					inBone = false;
					vWeights.Add(vertWeights);
					vertWeights = new BoneWeight[1];
					bons.Add(bones.ToArray());
					bones.Clear();
					binds.Add(bindPoses.ToArray());
					bindPoses.Clear();
				}
				continue;
			}
			if (!inBone)
			{
				boneParents.Add(nodes[i].parent);
			}
			inBone = true;
			BBoneData BBND = nodes[i].bone.BBND;
			if (BBND.vertex_id.Count > 0)
			{
				bonesExist = true;
			}
			for (int i2 = 0; i2 < BBND.vertex_id.Count; ++i2)
			{
				if (vertWeights.Length <= BBND.vertex_id[i2])
				{
					Array.Resize(ref vertWeights, BBND.vertex_id[i2] + 1);
				}
				if (vertWeights[BBND.vertex_id[i2]].weight0 == 0f)
				{
					vertWeights[BBND.vertex_id[i2]].weight0 = BBND.weight[i2];
					vertWeights[BBND.vertex_id[i2]].boneIndex0 = bones.Count;
				}
				else if (vertWeights[BBND.vertex_id[i2]].weight1 == 0f)
				{
					vertWeights[BBND.vertex_id[i2]].weight1 = BBND.weight[i2];
					vertWeights[BBND.vertex_id[i2]].boneIndex1 = bones.Count;
				}
				else if (vertWeights[BBND.vertex_id[i2]].weight2 == 0f)
				{
					vertWeights[BBND.vertex_id[i2]].weight2 = BBND.weight[i2];
					vertWeights[BBND.vertex_id[i2]].boneIndex2 = bones.Count;
				}
				else if (vertWeights[BBND.vertex_id[i2]].weight3 == 0f)
				{
					vertWeights[BBND.vertex_id[i2]].weight3 = BBND.weight[i2];
					vertWeights[BBND.vertex_id[i2]].boneIndex3 = bones.Count;
				}
			}
			GameObject newBone = new GameObject();
			newBone.name = nodes[i].name;
			nodes[i].obj = newBone;
			if (nodes[i].parent != null && nodes[i].parent.obj != null)
			{
				newBone.transform.parent = nodes[i].parent.obj.transform;
			}
			else
			{
				newBone.transform.parent = boneParent.transform;
			}
			newBone.transform.localPosition = nodes[i].pos;
			newBone.transform.localRotation = nodes[i].rot;
			newBone.transform.localScale = nodes[i].scale;
			bindPoses.Add(newBone.transform.worldToLocalMatrix * transform.localToWorldMatrix);
			bones.Add(newBone.transform);
			--i;
		}
		if (inBone)
		{
			vWeights.Add(vertWeights);
			bons.Add(bones.ToArray());
			binds.Add(bindPoses.ToArray());
		}
		i = nodes.Count - 1;
		while (i >= 0)
		{
			if (nodes[i].mesh == null || nodes[i].mesh.name != "MESH")
			{
				--i;
				continue;
			}
			bool bonesExistUs = false;
			BoneWeight[] bw = new BoneWeight[1];
			Transform[] bon = new Transform[1];
			Matrix4x4[] bind = new Matrix4x4[1];
			if (bonesExist)
			{
				for (int i2 = 0; i2 < boneParents.Count; ++i2)
				{
					if (boneParents[i2] == nodes[i])
					{
						bonesExistUs = true;
						bw = vWeights[i2];
						bon = bons[i2];
						bind = binds[i2];
						i2 = boneParents.Count;
					}
				}
			}
			BMeshData BMD = nodes[i].mesh.BMD;
			// valid material, create single mesh
			if (BMD.brush_id != -1)
			{
				Mesh m = new Mesh();

				BVertData BVD = nodes[i].mesh.BVD;

				if (BVD.SVD.Count > 65536)
				{
					m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
				}

				objs[i] = new GameObject();

				objs[i].layer = rendLayer;

				objs[i].transform.parent = gameObject.transform;

				// verts
				vertpos = new Vector3[BVD.SVD.Count];
				uvs = new Vector2[BVD.SVD.Count];
				if (BVD.tex_coord_sets > 1)
				{
					uvs2 = new Vector2[BVD.SVD.Count];
				}
				norms = new Vector3[BVD.SVD.Count];
				Color[] vcolors = new Color[BVD.SVD.Count];

				int i2 = 0;
				while (i2 < BVD.SVD.Count)
				{
					vertpos[i2] = BVD.SVD[i2].pos;
					uvs[i2].x = BVD.SVD[i2].Tex_Coords[0][0];
					uvs[i2].y = BVD.SVD[i2].Tex_Coords[0][1];
					if (BVD.tex_coord_sets > 1)
					{
						uvs2[i2].x = BVD.SVD[i2].Tex_Coords[1][0];
						uvs2[i2].y = BVD.SVD[i2].Tex_Coords[1][1];
					}
					norms[i2] = BVD.SVD[i2].normal;
					vcolors[i2] = new Color(BVD.SVD[i2].r, BVD.SVD[i2].g, BVD.SVD[i2].b, BVD.SVD[i2].a);
					i2++;
				}

				m.vertices = vertpos;
				m.uv = uvs;
				if (BVD.tex_coord_sets > 1)
				{
					m.uv2 = uvs2;
				} else
				{
					m.uv2 = uvs;
				}
				m.normals = norms;
				m.colors = vcolors;

				BoneWeight[] bw2;

				if (bw.Length != BVD.SVD.Count)
				{
					bw2 = new BoneWeight[BVD.SVD.Count];
					Array.Copy(bw, bw2, Mathf.Min(BVD.SVD.Count, bw.Length));
				}
				else
				{
					bw2 = bw;
				}

				if (bonesExistUs)
				{
					m.boneWeights = bw2;
					m.bindposes = bind;
				}

				// triangle indexes
				List<int> inds = new List<int>();

				i2 = 0;
				while (i2 < BMD.tris.Count)
				{
					BTriData BTD = BMD.tris[i2].BTRD;
					int i3 = 0;
					while (i3 < BTD.tri_ind.Count)
					{
						inds.Add(BTD.tri_ind[i3]);
						i3++;
					}
					i2++;
				}

				m.triangles = inds.ToArray();

				meshs.Add(m);

				if (vis)
				{
					if (!bonesExistUs)
					{
						MeshFilter mf = objs[i].AddComponent<MeshFilter>();
						mf.mesh = m;
						MeshRenderer mr = objs[i].AddComponent<MeshRenderer>();
						if (castShadows)
						{
							mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
						}
						else
						{
							mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
						}
						mr.material = mats[BMD.brush_id];
					}
					else
					{
						SkinnedMeshRenderer smr = objs[i].AddComponent<SkinnedMeshRenderer>();
						if (castShadows)
						{
							smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
						}
						else
						{
							smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
						}
						smr.material = mats[BMD.brush_id];
						smr.bones = bon;
						smr.sharedMesh = m;
					}
				}
				if (col)
				{
					MeshCollider mc = objs[i].AddComponent<MeshCollider>();
					mc.sharedMesh = m;
				}
				objs[i].transform.parent = nodes[i].parent.obj.transform;
				objs[i].transform.localScale = flip(nodes[i].scale * 0.17f);
				objs[i].transform.localPosition = flip(nodes[i].pos * 0.17f);
				objs[i].transform.localRotation = flip(nodes[i].rot);
				nodes[i].obj = objs[i];
				objs[i].name = nodes[i].name;
			}
			// invalid material, create a new mesh for each triangle group
			else
			{
				objs[i] = new GameObject();

				objs[i].layer = rendLayer;

				objs[i].transform.parent = gameObject.transform;

				BVertData BVD = nodes[i].mesh.BVD;
				// verts
				vertpos = new Vector3[BVD.SVD.Count];
				uvs = new Vector2[BVD.SVD.Count];
				if (BVD.tex_coord_sets > 1)
				{
					uvs2 = new Vector2[BVD.SVD.Count];
				}
				norms = new Vector3[BVD.SVD.Count];
				Color[] vcolors = new Color[BVD.SVD.Count];

				int i2 = 0;
				while (i2 < BVD.SVD.Count)
				{
					vertpos[i2] = BVD.SVD[i2].pos;
					uvs[i2].x = BVD.SVD[i2].Tex_Coords[0][0];
					uvs[i2].y = BVD.SVD[i2].Tex_Coords[0][1];
					if (BVD.tex_coord_sets > 1)
					{
						uvs2[i2].x = BVD.SVD[i2].Tex_Coords[1][0];
						uvs2[i2].y = BVD.SVD[i2].Tex_Coords[1][1];
					}
					norms[i2] = BVD.SVD[i2].normal;
					vcolors[i2] = new Color(BVD.SVD[i2].r, BVD.SVD[i2].g, BVD.SVD[i2].b, BVD.SVD[i2].a);
					++i2;
				}

				i2 = 0;

				Mesh m = new Mesh();
				if (vertpos.Length > 65536)
				{
					m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
				}
				m.vertices = vertpos;
				m.uv = uvs;
				if (BVD.tex_coord_sets > 1)
				{
					m.uv2 = uvs2;
				} else
				{
					m.uv2 = uvs;
				}
				m.colors = vcolors;

				BoneWeight[] bw2;

				if (bw.Length != BVD.SVD.Count)
				{
					bw2 = new BoneWeight[BVD.SVD.Count];
					Array.Copy(bw, bw2, Mathf.Min(BVD.SVD.Count, bw.Length));
				} else
				{
					bw2 = bw;
				}

				// assign bone weights and bind pose
				if (bonesExistUs)
				{
					m.boneWeights = bw2;
					m.bindposes = bind;
				}

				m.subMeshCount = BMD.tris.Count;
				// create new material list
				Material[] lmats = new Material[BMD.tris.Count];
				while (i2 < BMD.tris.Count)
				{
					BTriData BTD = BMD.tris[i2].BTRD;
					m.SetTriangles(BTD.tri_ind.ToArray(), i2);
					if (BTD.brush_id >= 0 && BTD.brush_id < mats.Length)
					{
						lmats[i2] = mats[BTD.brush_id];
					} else
					{
						lmats[i2] = new Material(Shader.Find("Standard"));
					}
					i2++;
				}
				if ((BVD.flags & 1) != 0)
				{
					m.normals = norms;
				}
				else
				{
					m.RecalculateNormals();
				}
				GameObject newobj = objs[i];
				newobj.layer = rendLayer;
				meshs.Add(m);
				// generate mesh differently based on whether we're using an animated mesh or regular mesh
				if (vis)
				{
					if (!bonesExistUs)
					{
						MeshFilter mf = newobj.AddComponent<MeshFilter>();
						mf.mesh = m;
						MeshRenderer mr = newobj.AddComponent<MeshRenderer>();
						if (castShadows)
						{
							mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
						}
						else
						{
							mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
						}
						mr.materials = lmats;
					} else
					{
						SkinnedMeshRenderer smr = newobj.AddComponent<SkinnedMeshRenderer>();
						if (castShadows)
						{
							smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
						} else
						{
							smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
						}
						smr.materials = lmats;
						smr.bones = bon;
						smr.sharedMesh = m;
					}
				}
				if (col)
				{
					MeshCollider mc = newobj.AddComponent<MeshCollider>();
					mc.sharedMesh = m;
				}
				if (nodes[i].parent != null && nodes[i].parent.obj != null)
				{
					newobj.transform.parent = nodes[i].parent.obj.transform;
				} else
				{
					newobj.transform.parent = gameObject.transform;
				}
				newobj.transform.localScale = nodes[i].scale;
				newobj.transform.localPosition = nodes[i].pos;
				newobj.transform.localRotation = nodes[i].rot;
				nodes[i].obj = newobj;
				// ROTATE -90 x 180 y
				newobj.name = nodes[i].name;
			}
			--i;
		}

		if (animSize > 0)
		{
			// one last time, iterate over all nodes to generate animation off of them
			Animation anim = gameObject.AddComponent<Animation>();
			AnimationClip clip = new AnimationClip();
			clip.name = "Animation";
			clip.legacy = true;

			i = nodes.Count - 1;
			while (i >= 0)
			{
				if (nodes[i].anim != null && nodes[i].obj != null)
				{
					BKeysData BKD = nodes[i].anim;
					List<Keyframe> px = new List<Keyframe>();
					List<Keyframe> py = new List<Keyframe>();
					List<Keyframe> pz = new List<Keyframe>();
					List<Keyframe> sx = new List<Keyframe>();
					List<Keyframe> sy = new List<Keyframe>();
					List<Keyframe> sz = new List<Keyframe>();
					List<Keyframe> rx = new List<Keyframe>();
					List<Keyframe> ry = new List<Keyframe>();
					List<Keyframe> rz = new List<Keyframe>();
					List<Keyframe> rw = new List<Keyframe>();

					int len = Mathf.Max(BKD.rot.Length, BKD.pos.Length, BKD.scale.Length);

					for (int i2 = 0; i2 < len; ++i2)
					{
						if ((BKD.animFlags & 1) != 0 && i2 < BKD.usedFrameP.Length && BKD.usedFrameP[i2])
						{
							px.Add(new Keyframe(i2 / 24f, BKD.pos[i2].x));
							py.Add(new Keyframe(i2 / 24f, BKD.pos[i2].y));
							pz.Add(new Keyframe(i2 / 24f, BKD.pos[i2].z));
						}
						if ((BKD.animFlags & 2) != 0 && i2 < BKD.usedFrameS.Length && BKD.usedFrameS[i2])
						{
							sx.Add(new Keyframe(i2 / 24f, BKD.scale[i2].x));
							sy.Add(new Keyframe(i2 / 24f, BKD.scale[i2].y));
							sz.Add(new Keyframe(i2 / 24f, BKD.scale[i2].z));
						}
						if ((BKD.animFlags & 4) != 0 && i2 < BKD.usedFrameR.Length && BKD.usedFrameR[i2])
						{
							rx.Add(new Keyframe(i2 / 24f, BKD.rot[i2].x));
							ry.Add(new Keyframe(i2 / 24f, BKD.rot[i2].y));
							rz.Add(new Keyframe(i2 / 24f, BKD.rot[i2].z));
							rw.Add(new Keyframe(i2 / 24f, BKD.rot[i2].w));
						}
					}
					AnimationCurve curve;
					string name = GetRelativePath(nodes[i].obj);
					if ((BKD.animFlags & 1) != 0)
					{
						curve = new AnimationCurve(px.ToArray());
						clip.SetCurve(name, typeof(Transform), "localPosition.x", curve);
						// end me
						curve = new AnimationCurve(py.ToArray());
						clip.SetCurve(name, typeof(Transform), "localPosition.y", curve);
						curve = new AnimationCurve(pz.ToArray());
						clip.SetCurve(name, typeof(Transform), "localPosition.z", curve);
					}
					if ((BKD.animFlags & 2) != 0)
					{
						curve = new AnimationCurve(sx.ToArray());
						clip.SetCurve(name, typeof(Transform), "localScale.x", curve);
						curve = new AnimationCurve(sy.ToArray());
						clip.SetCurve(name, typeof(Transform), "localScale.y", curve);
						curve = new AnimationCurve(sz.ToArray());
						clip.SetCurve(name, typeof(Transform), "localScale.z", curve);
					}
					if ((BKD.animFlags & 4) != 0)
					{
						curve = new AnimationCurve(rx.ToArray());
						clip.SetCurve(name, typeof(Transform), "localRotation.x", curve);
						curve = new AnimationCurve(ry.ToArray());
						clip.SetCurve(name, typeof(Transform), "localRotation.y", curve);
						curve = new AnimationCurve(rz.ToArray());
						clip.SetCurve(name, typeof(Transform), "localRotation.z", curve);
						curve = new AnimationCurve(rw.ToArray());
						clip.SetCurve(name, typeof(Transform), "localRotation.w", curve);
					}
				}
				--i;
			}
			clip.wrapMode = WrapMode.Loop;
			anim.AddClip(clip, clip.name);
			anim.Play("Animation");
		}
		gameObject.transform.position = pos * 0.17f;
        gameObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 180f, 0f) * flip(Quaternion.Euler(rot));
        gameObject.transform.localScale = new Vector3(-scale.x * 0.17f, scale.z * 0.17f, scale.y * 0.17f);
        // let's clear our nodes to free memory
        brushes = null;
        texs = null;
        nodes = null;
    }
}

/*
 * Documentation for future generations who wish to preserve this
 * Y and Z are all reversed in all vectors and quaternions, and models scaled to -1 X
B3D doc:
chunks
each chunk is:
tag[4]
int length

BB3D chunk:
int version
TEXS chunk
BRUS chunk
NODE chunk
all optional ^

TEXS:
{
string texture file name
int flags
	color
	alpha
	masked
	mipmap
	clamp U
	clamp V
	spherical
	cubic
int blend
		BLEND_REPLACE=	0,
		BLEND_ALPHA=	1,
		BLEND_MULTIPLY=	2,
		BLEND_ADD=		3,
		BLEND_DOT3=		4,
		BLEND_MULTIPLY2=5,
float x_pos, y_pos (x/y offsets i guess)
float x_scale,y_scale (obvs)
float rotation (rot in radians)
}

if flags field & 65536 then it indicates the texture has secondary UVs

BRUS:
int tex_num (numtextures ofc)
{
string name (apparently texture name by default)
float r,g,b,a (colors for material?)
float shininess (obvs)
int blend, fx (??)
int texture_id[tex_num] (textures used in brush)
}

NODE:
string name
float position[3]
float scale[3]
quat rotation (wxyz)
MESH or BONE chunk
optional KEYS chunk
optional NODE chunk
optional ANIM chunk

other chunks:
VRTS:
int flags (1 = normals, 2 = rgba)
int tex_coord_sets (number of texture coords per vertex)
int tex_coord_set_size (components per set)
{
float x,y,z (obvs)
float nx,ny,nz (vert normals, see flags)
float r,g,b,a (vert colors, see flags)
float tex_coords[tex_coord_sets][tex_coord_set_size] (obvs)
}

TRIS:
int brush_id (brush applied to tris)
{
int vert_id[3] (vert indexes, obvs)
}

MESH:
int brush_id
something to note: the below 2 lines are how the official specifications define it, however the official implementation actually implements it differently
VRTS
TRIS[x] (can be more than one TRIS chunk)
the official implementation dictates that there's simply an array of chunks, and they can be VRTS OR TRIS, and multiple VRTS chunks would be concatenated together like TRIS chunks

repeat for length of bone
BONE:
{
int vert_id (vertex affected by bone)
float weight (obvs)
}

KEYS:
int flags (1 = position, 2 = scale, 4 = rotation)
{
int frame
float position[3]
float scale[3]
float rotation[4] (for all these, refer to flags value)
}

ANIM:
int flags (unused lol)
int frames (obvs)
float fps (obvs, though actually unused in Blitz3D!)
 */
