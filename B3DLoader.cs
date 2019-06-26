using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class B3DLoader : MonoBehaviour {

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
        public BChunk mesh; // could also be bone
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
    List<Material> matss;

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

    int its = 0;

    void ProcessChunk(BChunk chunk, FileStream fs)
    {
        if (its > 100)
        {
           // return;
        } else
        {
            its++;
        }
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
                        STD.scale.y = ReadFloat(fs);
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
                    BND.rot = new Quaternion(ReadFloat(fs), ReadFloat(fs), ReadFloat(fs), ReadFloat(fs));
                    // relative scale and position
                    if (chunk.parent.name == "NODE")
                    {
                        BND.pos += chunk.parent.BND.pos;
                        BND.scale = new Vector3(BND.scale.x * chunk.parent.BND.scale.x, BND.scale.y * chunk.parent.BND.scale.y, BND.scale.z * chunk.parent.BND.scale.z);
                        BND.rot = chunk.parent.BND.rot * BND.rot;
                    }
                    chunk.BND = BND;
                    while (fs.Position < chunk.start + chunk.length)
                    {
                        BChunk msh = ReadChunk(chunk, fs);
                        if (msh.name == "MESH")
                        {
                            BND.mesh = msh;
                        }
                    }
                    // here you'd normally check for KEYS, NODE, and ANIM chunks. i'm gonna not, though, because no anim support
                    nodes.Add(BND);
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
                        SVD.pos = new Vector3(ReadFloat(fs), ReadFloat(fs), ReadFloat(fs));
                        if ((BVD.flags & 1) != 0)
                        {
                            SVD.normal = new Vector3(ReadFloat(fs), ReadFloat(fs), ReadFloat(fs));
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
                    }
                    chunk.BTRD = BTD;
                }
                break;

            // bones, keys, and anims aren't used!
            default:
                {
                    fs.Seek(chunk.length + chunk.start, SeekOrigin.Begin);
                }
                break;
        }
    }

    // this nukes everything when ever any b3d is destroyed. since b3ds are usually only destroyed at stage end, though, i don't care.
    private void OnDestroy()
    {
        // free memory
        for (int i = 0; i < meshs.Count; i++)
        {
            Destroy(meshs[i]);
        }
        for (int i = 0; i < tex2ds.Count; i++)
        {
            Destroy(tex2ds[i]);
        }
        for (int i = 0; i < matss.Count; i++)
        {
            Destroy(matss[i]);
        }
        // nuke the cache
        textureCache = new Dictionary<string, TextureRefs>();
    }

    public void LoadB3D(string filename, bool vis, bool col, Vector3 pos, Vector3 scale, Vector3 rot, int rendLayer = 0)
    {
        FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
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

        FinishB3DLoad(filename, vis, col, pos, scale, rot, rendLayer);
        return;
    }
	
   void FinishB3DLoad(string filename, bool vis, bool col, Vector3 pos, Vector3 scale, Vector3 rot, int rendLayer)
    {
        // textures
        byte[] fn = System.Text.Encoding.ASCII.GetBytes(filename);
        int i = fn.Length - 1;
        while (fn[i] != 0x2F && fn[i] != 0x5C)
        {
            i--;
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
            while (tfn[i2] != 0x2F && tfn[i2] != 0x5C && i2 != 0)
            {
                i2--;
            }
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
                    try {
                        img = File.Open(string.Concat(Application.dataPath, "/../", cutFileName, newTexPath), FileMode.Open);
                    } catch
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
                if (rendLayer != 12)
                {
                    mats[i] = new Material(Shader.Find("Standard"));
                    matss.Add(mats[i]);
                }
                else
                {
                    mats[i] = new Material(Shader.Find("Unlit/Texture"));
                    matss.Add(mats[i]);
                }
                // fade rendering iirc
                if (brushes[0].SBD[i].a != 1.0f)
                {
                    mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mats[i].SetInt("_ZWrite", 0);
                    mats[i].DisableKeyword("_ALPHATEST_ON");
                    mats[i].EnableKeyword("_ALPHABLEND_ON");
                    mats[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mats[i].renderQueue = 3000;
                }
                mats[i].SetColor("_Color", new Color(brushes[0].SBD[i].r, brushes[0].SBD[i].g, brushes[0].SBD[i].b, brushes[0].SBD[i].a));
                SubBrusData SBD = brushes[0].SBD[i];
                if (SBD.texture_id.Length != 0 && SBD.texture_id[0] != -1)
                {
                    mats[i].mainTexture = tex[SBD.texture_id[0]];
                    mats[i].mainTextureScale = new Vector2(texs[0].STD[SBD.texture_id[0]].scale.x, -texs[0].STD[SBD.texture_id[0]].scale.y);
                    mats[i].mainTextureOffset = texs[0].STD[SBD.texture_id[0]].pos;
                }
                else
                {
                    mats[i].mainTexture = null;
                }
                mats[i].SetFloat("_Glossiness", SBD.shininess);
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
        i = 0;
        Vector3[] vertpos;
        Vector2[] uvs;
        Vector3[] norms;
        while (i < nodes.Count)
        {
            if (nodes[i].mesh == null || nodes[i].mesh.name != "MESH")
            {
                i++;
                continue;
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
                norms = new Vector3[BVD.SVD.Count];

                int i2 = 0;
                while (i2 < BVD.SVD.Count)
                {
                    vertpos[i2] = BVD.SVD[i2].pos;
                    uvs[i2].x = BVD.SVD[i2].Tex_Coords[0][0];
                    uvs[i2].y = BVD.SVD[i2].Tex_Coords[0][1];
                    norms[i2] = BVD.SVD[i2].normal;
                    i2++;
                }

                m.vertices = vertpos;
                m.uv = uvs;
                m.normals = norms;

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

                MeshFilter mf = objs[i].AddComponent<MeshFilter>();
                mf.mesh = m;
                if (vis)
                {
                    MeshRenderer mr = objs[i].AddComponent<MeshRenderer>();
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                    mr.material = mats[BMD.brush_id];
                }
                if (col)
                {
                    MeshCollider mc = objs[i].AddComponent<MeshCollider>();
                    mc.sharedMesh = m;
                    objs[i].AddComponent<CollisionCache>();
                }
                objs[i].transform.localScale = nodes[i].scale * 0.17f;
                objs[i].transform.position = nodes[i].pos * 0.17f;
                //objs[i].transform.rotation = nodes[i].rot;
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
                norms = new Vector3[BVD.SVD.Count];

                int i2 = 0;
                while (i2 < BVD.SVD.Count)
                {
                    vertpos[i2] = BVD.SVD[i2].pos;
                    uvs[i2].x = BVD.SVD[i2].Tex_Coords[0][0];
                    uvs[i2].y = BVD.SVD[i2].Tex_Coords[0][1];
                    norms[i2] = BVD.SVD[i2].normal;
                    i2++;
                }

                i2 = 0;

                Mesh m = new Mesh();
                if (vertpos.Length > 65536)
                {
                    m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                }
                m.vertices = vertpos;
                m.uv = uvs;
                m.normals = norms;
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
                GameObject newobj = objs[i];
                newobj.layer = rendLayer;
                newobj.transform.parent = objs[i].transform;
                meshs.Add(m);
                MeshFilter mf = newobj.AddComponent<MeshFilter>();
                mf.mesh = m;
                if (vis)
                {
                    MeshRenderer mr = newobj.AddComponent<MeshRenderer>();
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                    mr.materials = lmats;
                }
                if (col)
                {
                    MeshCollider mc = newobj.AddComponent<MeshCollider>();
                    mc.sharedMesh = m;
                    newobj.AddComponent<CollisionCache>();
                }
                newobj.transform.localScale = nodes[i].scale * 0.17f;
                newobj.transform.position = nodes[i].pos * 0.17f;
                newobj.name = nodes[i].name;
            }
            i++;
        }
        gameObject.transform.position = pos;
        gameObject.transform.rotation = Quaternion.Euler(rot);
        gameObject.transform.localScale = scale;
        // let's clear our nodes to free memory
        brushes = null;
        texs = null;
        nodes = null;
    }
}

/*
 * Documentation for future generations who wish to preserve this
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
int flags (??)
int blend (??)
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
float rotation[4]
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

only writing bone related stuff here for preservation purposes
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
float fps (obvs)
 */
