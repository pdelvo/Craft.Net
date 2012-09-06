using LibNbt;
using LibNbt.Tags;

namespace Craft.Net.Data
{
    /// <summary>
    /// Used to represent a 16x256x16 chunk of blocks in
    /// a <see cref="Craft.Net.Data.World"/>.
    /// </summary>
    public class Chunk
    {
        public const int Width = 16, Height = 256, Depth = 16;
        internal bool IsModified;

        /// <summary>
        /// Creates a new Chunk within the specified <see cref="Craft.Net.Data.Region"/>
        /// at the specified location.
        /// </summary>
        public Chunk(Vector3 relativePosition, Region parentRegion)
        {
            Sections = new Section[16];
            for (int i = 0; i < Sections.Length; i++)
                Sections[i] = new Section((byte)i);
            RelativePosition = relativePosition;
            Biomes = new byte[Width * Depth];
            HeightMap = new int[Width * Depth];
            ParentRegion = parentRegion;
            IsModified = false;
        }

        /// <summary>
        /// Creates a new chunk at the specified location.
        /// </summary>
        public Chunk(Vector3 relativePosition)
        {
            Sections = new Section[16];
            for (int i = 0; i < Sections.Length; i++)
                Sections[i] = new Section((byte)i);
            RelativePosition = relativePosition;
            Biomes = new byte[Width * Depth];
            HeightMap = new int[Width * Depth];
        }

        /// <summary>
        /// The location of this chunk in global,
        /// world-wide coordinates.
        /// </summary>
        public Vector3 AbsolutePosition 
        {
            get { return (ParentRegion.Position * new Vector3(Region.Width, 0, Region.Depth)) + RelativePosition; }
        }

        /// <summary>
        /// The biome data for this chunk.
        /// </summary>
        public byte[] Biomes { get; set; }

        /// <summary>
        /// Each byte corresponds to the height of the given 1x256x1
        /// column of blocks.
        /// </summary>
        public int[] HeightMap { get; set; }

        /// <summary>
        /// The region this chunk is contained in.
        /// </summary>
        public Region ParentRegion { get; set; }

        /// <summary>
        /// The position of this chunk within the parent region.
        /// </summary>
        public Vector3 RelativePosition { get; set; }

        /// <summary>
        /// The 16x16x16 sections that make up this chunk.
        /// </summary>
        public Section[] Sections { get; set; }

        /// <summary>
        /// Sets the value of the block at the given position, relative to this chunk.
        /// </summary>
        public void SetBlock(Vector3 position, Block value)
        {
            var y = (byte)position.Y;
            y /= 16;
            position.Y = position.Y % 16;
            Sections[y].SetBlock(position, value);
            var heightIndex = (byte)(position.Z * Depth) + (byte)position.X;
            if (HeightMap[heightIndex] < position.Y)
                HeightMap[heightIndex] = (byte)position.Y;
            IsModified = true;
        }

        /// <summary>
        /// Gets the block at the given position, relative to this chunk.
        /// </summary>
        public Block GetBlock(Vector3 position)
        {
            var y = (byte)position.Y;
            y /= 16;
            position.Y = position.Y % 16;
            return Sections[y].GetBlock(position);
        }

        /// <summary>
        /// Gets the biome at the given column.
        /// </summary>
        public Biome GetBiome(byte x, byte z)
        {
            return (Biome)Biomes[(byte)(z * Depth) + x];
        }

        /// <summary>
        /// Sets the value of the biome at the given column.
        /// </summary>
        public void SetBiome(byte x, byte z, Biome value)
        {
            Biomes[(byte)(z * Depth) + x] = (byte)value;
            IsModified = true;
        }

        /// <summary>
        /// Gets the height of the specified column.
        /// </summary>
        public int GetHeight(byte x, byte z)
        {
            return HeightMap[(byte)(z * Depth) + x];
        }

        public NbtFile ToNbt() // TODO: Entities
        {
            NbtFile file = new NbtFile();
            NbtCompound level = new NbtCompound("Level");
            
            // Entities // TODO
            level.Tags.Add(new NbtList("Entities"));

            // Biomes
            level.Tags.Add(new NbtByteArray("Biomes", Biomes));

            // Last Update // TODO: What is this
            level.Tags.Add(new NbtLong("LastUpdate", 15));

            // Position
            level.Tags.Add(new NbtInt("xPos", (int)AbsolutePosition.X));
            level.Tags.Add(new NbtInt("zPos", (int)AbsolutePosition.Z));

            // Tile Entities // TODO
            level.Tags.Add(new NbtList("TileEntites"));

            // Terrain Populated // TODO: When is this 0? Will vanilla use this?
            level.Tags.Add(new NbtByte("TerrainPopualted", 0));

            // Sections and height
            level.Tags.Add(new NbtIntArray("HeightMap", HeightMap));
            NbtList sectionList = new NbtList("Sections");
            foreach (var section in Sections)
            {
                if (!section.IsAir)
                {
                    NbtCompound sectionTag = new NbtCompound();
                    sectionTag.Tags.Add(new NbtByte("Y", section.Y));
                    sectionTag.Tags.Add(new NbtByteArray("Blocks", section.Blocks));
                    sectionTag.Tags.Add(new NbtByteArray("Data", section.Metadata.Data));
                    sectionTag.Tags.Add(new NbtByteArray("SkyLight", section.SkyLight.Data));
                    sectionTag.Tags.Add(new NbtByteArray("BlockLight", section.BlockLight.Data));
                    sectionList.Tags.Add(sectionTag);
                }
            }
            level.Tags.Add(sectionList);

            var rootCompound = new NbtCompound();
            rootCompound.Tags.Add(level);
            file.RootTag = rootCompound;

            return file;
        }

        public static Chunk FromNbt(Vector3 position, NbtFile nbt)
        {
            Chunk chunk = new Chunk(position);
            // Load data
            var root = nbt.RootTag.Get<NbtCompound>("Level");
            chunk.Biomes = root.Get<NbtByteArray>("Biomes").Value;
            chunk.HeightMap = root.Get<NbtIntArray>("HeightMap").Value;
            var sections = root.Get<NbtList>("Sections");
            foreach (var sectionTag in sections.Tags)
            {
                // Load data
                var compound = (NbtCompound)sectionTag;
                byte y = compound.Get<NbtByte>("Y").Value;
                var section = new Section(y);
                section.Blocks = compound.Get<NbtByteArray>("Blocks").Value;
                section.BlockLight.Data = compound.Get<NbtByteArray>("BlockLight").Value;
                section.SkyLight.Data = compound.Get<NbtByteArray>("SkyLight").Value;
                section.Metadata.Data = compound.Get<NbtByteArray>("Data").Value;
                // Process section
                section.ProcessSection();
                chunk.Sections[y] = section;
            }
            return chunk;
        }
    }
}