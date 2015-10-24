﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Transit.Framework;
using Transit.Framework.Builders;
using Transit.Framework.Modularity;
using UnityEngine;

#if DEBUG
using Debug = Transit.Framework.Debug;
#endif

namespace Transit.Addon.RoadExtensions
{
    public partial class RExModule
    {
        [UsedImplicitly]
        private class RoadsInstaller : Installer<RExModule>
        {
            protected override bool ValidatePrerequisites()
            {
                if (!LocalizationInstaller.Done)
                {
                    return false;
                }

                if (!AssetsInstaller.Done)
                {
                    return false;
                }

                var roadObject = GameObject.Find(ROAD_NETCOLLECTION);
                if (roadObject == null)
                {
                    return false;
                }

                var netColl = FindObjectsOfType<NetCollection>();
                if (netColl == null || !netColl.Any())
                {
                    return false;
                }

                var roadCollFound = false;
                foreach (var col in netColl)
                {
                    if (col.name == ROAD_NETCOLLECTION)
                    {
                        roadCollFound = true;
                    }
                }

                if (!roadCollFound)
                {
                    return false;
                }

                return true;
            }

            protected override void Install(RExModule host)
            {
                Loading.QueueAction(() =>
                {
                    // PropInfo Builders -----------------------------------------------------------
                    var newInfos = new List<PropInfo>();

                    var piBuilders = host.Parts
                        .OfType<IPrefabBuilder<PropInfo>>()
                        .WhereActivated()
                        .ToArray();

                    foreach (var builder in piBuilders)
                    {
                        try
                        {
                            newInfos.Add(builder.Build());

                            Debug.Log(string.Format("REx: Prop {0} installed", builder.Name));
                        }
                        catch (Exception ex)
                        {
                            Debug.Log(string.Format("REx: Crashed-Prop builders {0}", builder.Name));
                            Debug.Log("REx: " + ex.Message);
                            Debug.Log("REx: " + ex.ToString());
                        }
                    }

                    var props = host._props = host._container.AddComponent<PropCollection>();
                    props.name = REX_PROPCOLLECTION;
                    if (newInfos.Count > 0)
                    {
                        props.m_prefabs = newInfos.ToArray();
                        PrefabCollection<PropInfo>.InitializePrefabs(props.name, props.m_prefabs, new string[] { });
                        PrefabCollection<PropInfo>.BindPrefabs();
                    }
                });

                Loading.QueueAction(() =>
                {
                    // NetInfo Builders -----------------------------------------------------------
                    var newInfos = new List<NetInfo>();

                    var niBuilders = host.Parts
                        .OfType<INetInfoBuilder>()
                        .WhereActivated()
                        .ToArray();

                    foreach (var builder in niBuilders)
                    {
                        try
                        {
                            newInfos.AddRange(builder.Build());

                            Debug.Log(string.Format("REx: {0} installed", builder.Name));
                        }
                        catch (Exception ex)
                        {
                            Debug.Log(string.Format("REx: Crashed-Network builders {0}", builder.Name));
                            Debug.Log("REx: " + ex.Message);
                            Debug.Log("REx: " + ex.ToString());
                        }
                    }

                    var roads = host._roads = host._container.AddComponent<NetCollection>();
                    roads.name = REX_NETCOLLECTION;
                    if (newInfos.Count > 0)
                    {
                        roads.m_prefabs = newInfos.ToArray();
                        PrefabCollection<NetInfo>.InitializePrefabs(roads.name, roads.m_prefabs, new string[] { });
                        PrefabCollection<NetInfo>.BindPrefabs();
                    }


                    // NetInfo Modifiers ----------------------------------------------------------
                    var modifiers = host.Parts
                        .OfType<INetInfoModifier>()
                        .WhereActivated()
                        .ToArray();

                    foreach (var modifier in modifiers)
                    {
                        try
                        {
                            modifier.ModifyExistingNetInfo();

                            Debug.Log(string.Format("REx: {0} modifications applied", modifier.Name));
                        }
                        catch (Exception ex)
                        {
                            Debug.Log(string.Format("REx: Crashed-Network modifiers {0}", modifier.Name));
                            Debug.Log("REx: " + ex.Message);
                            Debug.Log("REx: " + ex.ToString());
                        }
                    }


                    // Cross mods support -------------------------------------------------------------
                    var compParts = host.Parts
                        .OfType<ICompatibilityPart>()
                        .ToArray();

                    foreach (var compatibilityPart in compParts)
                    {
                        try
                        {
                            if (compatibilityPart.IsPluginActive)
                            {
                                compatibilityPart.Setup(newInfos);

                                Debug.Log(string.Format("REx: {0} compatibility activated", compatibilityPart.Name));
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Log(string.Format("REx: Crashed-CompatibilitySupport {0}", compatibilityPart.Name));
                            Debug.Log("REx: " + ex.Message);
                            Debug.Log("REx: " + ex.ToString());
                        }
                    }
                });
            }
        }
    }
}
