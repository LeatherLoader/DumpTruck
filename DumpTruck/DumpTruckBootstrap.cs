using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Facepunch;
using LeatherLoader;

namespace DumpTruck
{
    [Bootstrap]
    public class DumpTruckBootstrap : MonoBehaviour
    {
        private string configDir;

        public void Awake()
        {
            //A keep-alive for the game object so the script will survive long enough for OnLevelWasLoaded
            DontDestroyOnLoad(this.gameObject);
        }

        public void Start()
        {
            //Build config directory for where we're gonna output stuff
            configDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config"));
            //OnceLoaded gets called when all the bundles have finished loading into the game.  It's a good time to futz with the datablock dictionary
            //and other jerky things.
            Bundling.OnceLoaded += new Bundling.OnLoadedEventHandler(AssetsReady);
        }

        public void OnLevelWasLoaded (int level)
        {
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            //This one only works on server-side after the big wide world has been loaded, but it outputs all the crate spawners in the game
            DumpObjectSpawners();
        }

        public void AssetsReady()
        {
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            //Output all the loot tables in the game
            DumpLootLists();
            //Output a list of all item names
            DumpItems();
        }

        private void DumpObjectSpawners()
        {
            //Verify we have a directory to output stuff to
            string objectSpawnersDir = Path.Combine(configDir, "ObjectSpawners");

            if (!Directory.Exists(objectSpawnersDir))
            {
                Directory.CreateDirectory(objectSpawnersDir);
            }

            ConsoleSystem.Log(string.Format("Writing {0} spawnable objects.", UnityEngine.Object.FindObjectsOfType<LootableObjectSpawner>().Length));
            foreach (var spawner in UnityEngine.Object.FindObjectsOfType<LootableObjectSpawner>())
            {
                StringBuilder outputBuilder = new StringBuilder();
                outputBuilder.AppendLine("# X\tY\tZ\tMinRespawn\tMaxRespawn\tSpawnOnStart\tName\tID");
                outputBuilder.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", spawner.gameObject.transform.position.x, spawner.gameObject.transform.position.y, spawner.gameObject.transform.position.z, spawner.spawnTimeMin, spawner.spawnTimeMax, spawner.spawnOnStart, spawner.gameObject.name, spawner.gameObject.GetInstanceID());
                outputBuilder.AppendLine();
                outputBuilder.AppendLine();

                outputBuilder.AppendLine("# Prob\tName\tType\tLootList");
                foreach (var chance in spawner._lootableChances)
                {
                    string lootListName = "null";

                    if (chance.obj._spawnList != null)
                        lootListName = chance.obj._spawnList.name;

                    outputBuilder.AppendFormat("{0}\t{1}\t{2}\t{3}", chance.weight, chance.obj.name, chance.obj.GetType().Name, lootListName);
                    outputBuilder.AppendLine();
                }

                //All the spawners are actually named the same thing, so we need to incorporate their instanceID's too.
                string fileName = Path.ChangeExtension(Path.Combine(objectSpawnersDir, string.Concat(spawner.gameObject.name,"_",spawner.gameObject.GetInstanceID())), ".cfg");
                File.WriteAllText(fileName, outputBuilder.ToString());

				foreach (var chance in spawner._lootableChances)
				{
					String lootListFilename = Path.ChangeExtension(Path.Combine (objectSpawnersDir, string.Concat(spawner.gameObject.name,"_",spawner.gameObject.GetInstanceID(),"_",chance.obj._spawnList.name)), ".cfg");

					StringBuilder lootListBuilder = new StringBuilder();
					lootListBuilder.AppendLine("# Min\tMax\tNoDupes\tOneOfEach");
					lootListBuilder.AppendFormat(string.Format("{0}\t{1}\t{2}\t{3}", chance.obj._spawnList.minPackagesToSpawn, chance.obj._spawnList.maxPackagesToSpawn, chance.obj._spawnList.noDuplicates, chance.obj._spawnList.spawnOneOfEach));
					lootListBuilder.AppendLine();
					lootListBuilder.AppendLine();
					
					lootListBuilder.AppendLine("# Prob Weight\tT or I\tItem Name\tMin Count\tMax Count");
					if (chance.obj._spawnList.LootPackages != null)
					{
						foreach (var package in chance.obj._spawnList.LootPackages)
						{
							string type = "T";
							if (DatablockDictionary.GetByName(package.obj.name) != null)
								type = "I";
							lootListBuilder.AppendFormat(string.Format("{0}\t{1}\t{2}\t{3}\t{4}", package.weight, type, package.obj.name, package.amountMin, package.amountMax));
							lootListBuilder.AppendLine();
						}
					}
					File.WriteAllText(lootListFilename, lootListBuilder.ToString());
				}
            }
        }

        private void DumpItems()
        {
            StringBuilder builder = new StringBuilder();
            ConsoleSystem.Log(string.Format("Writing {0} items.", DatablockDictionary.All.Length));

            //The datablock dictionary has info on pretty much everything Rust-related.  Very interesting actually, item stats, etc.
            foreach (var item in DatablockDictionary.All)
            {
                builder.AppendLine(item.name);
            }
            string fileName = Path.ChangeExtension(Path.Combine(configDir, "items"), ".txt");
            File.WriteAllText(fileName, builder.ToString());
        }

        public void DumpLootLists()
        {
            //Verify directory exists to write to
            string lootListDir = Path.Combine(configDir, "LootLists");

            if (!Directory.Exists(lootListDir))
            {
                Directory.CreateDirectory(lootListDir);
            }

            ConsoleSystem.Log(string.Format("Writing {0} loot lists.", DatablockDictionary._lootSpawnLists.Count));
            foreach (string key in DatablockDictionary._lootSpawnLists.Keys)
            {
                LootSpawnList list = DatablockDictionary._lootSpawnLists[key];

                StringBuilder outputBuilder = new StringBuilder();

				if (list == null)
					outputBuilder.AppendLine("Empty");
				else 
				{
	                outputBuilder.AppendLine("# Min\tMax\tNoDupes\tOneOfEach");
	                outputBuilder.AppendFormat(string.Format("{0}\t{1}\t{2}\t{3}", list.minPackagesToSpawn, list.maxPackagesToSpawn, list.noDuplicates, list.spawnOneOfEach));
	                outputBuilder.AppendLine();
	                outputBuilder.AppendLine();

	                outputBuilder.AppendLine("# Prob Weight\tT or I\tItem Name\tMin Count\tMax Count");
	                foreach (var package in list.LootPackages)
	                {
						if (package == null)
						{
							outputBuilder.AppendLine("Empty");
						} else
						{
		                    string type = "T";
		                    if (package.obj == null || DatablockDictionary.GetByName(package.obj.name) != null)
		                        type = "I";

							String objName = "null";
							if (package.obj != null)
								objName = package.obj.name;
		                    outputBuilder.AppendFormat(string.Format("{0}\t{1}\t{2}\t{3}\t{4}", package.weight, type, objName, package.amountMin, package.amountMax));
		                    outputBuilder.AppendLine();
						}
	                }
				}

                string fileName = Path.ChangeExtension(Path.Combine(lootListDir, key), ".cfg");
                File.WriteAllText(fileName, outputBuilder.ToString());
            }
        }
    }
}
