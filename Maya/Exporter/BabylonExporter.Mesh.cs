﻿using Autodesk.Maya.OpenMaya;
using Autodesk.Maya.OpenMayaAnim;
using BabylonExport.Entities;
using MayaBabylon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Maya2Babylon
{
    internal partial class BabylonExporter
    {
        private MFnSkinCluster mFnSkinCluster;          // the skin cluster of the mesh/vertices
        private MFnTransform mFnTransform;              // the transform of the mesh
        private MStringArray allMayaInfluenceNames;     // the joint names that influence the mesh (joint with 0 weight included)
        private MDoubleArray allMayaInfluenceWeights;   // the joint weights for the vertex (0 weight included)
        private Dictionary<string, int> indexByNodeName = new Dictionary<string, int>();    // contains the node (joint and parents of the current skin) fullPathName and its index

        private bool hasMorphTarget = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mDagPath">DAG path to the transform</param>
        /// <param name="babylonScene"></param>
        /// <returns></returns>
        private BabylonNode ExportDummy(MDagPath mDagPath, BabylonScene babylonScene)
        {
            RaiseMessage(mDagPath.partialPathName, 1);
            
            MFnTransform mFnTransform = new MFnTransform(mDagPath);

            Print(mFnTransform, 2, "Print ExportDummy mFnTransform");

            var babylonMesh = new BabylonMesh { name = mFnTransform.name, id = mFnTransform.uuid().asString() };
            babylonMesh.isDummy = true;

            // Position / rotation / scaling / hierarchy
            ExportNode(babylonMesh, mFnTransform, babylonScene);

            // Animations
            ExportNodeAnimation(babylonMesh, mFnTransform);

            babylonScene.MeshesList.Add(babylonMesh);

            return babylonMesh;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mDagPath">DAG path to the transform above mesh</param>
        /// <param name="babylonScene"></param>
        /// <returns></returns>
        private BabylonNode ExportMesh(MDagPath mDagPath, BabylonScene babylonScene)
        {
            RaiseMessage(mDagPath.partialPathName, 1);

            // Transform above mesh
            mFnTransform = new MFnTransform(mDagPath);

            // Mesh direct child of the transform
            // TODO get the original one rather than the modified?
            MFnMesh mFnMesh = null;         // Shape of the mesh displayed by maya when the export begins. It contains the material, skin, blendShape information.
            MFnMesh meshShapeOrig = null;   // Original shape of the mesh
            for (uint i = 0; i < mFnTransform.childCount; i++)
            {
                MObject childObject = mFnTransform.child(i);
                if (childObject.apiType == MFn.Type.kMesh)
                {
                    var _mFnMesh = new MFnMesh(childObject);
                    if (!_mFnMesh.isIntermediateObject)
                    {
                        mFnMesh = _mFnMesh;
                    }
                    else
                    {
                        meshShapeOrig = _mFnMesh;
                    }
                }
            }

            if (meshShapeOrig != null)
            {
                hasMorphTarget = hasBlendShape(mFnMesh.objectProperty);
            }
            else
            {
                hasMorphTarget = false;
            }


            if (mFnMesh == null)
            {
                RaiseError("No mesh found has child of " + mDagPath.fullPathName);
                return null;
            }

            RaiseMessage("mFnMesh.fullPathName=" + mFnMesh.fullPathName, 2);

            // --- prints ---
            #region prints

            Action<MFnDagNode> printMFnDagNode = (MFnDagNode mFnDagNode) =>
           {
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.name=" + mFnDagNode.name, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.absoluteName=" + mFnDagNode.absoluteName, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.fullPathName=" + mFnDagNode.fullPathName, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.partialPathName=" + mFnDagNode.partialPathName, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.activeColor=" + mFnDagNode.activeColor.toString(), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.attributeCount=" + mFnDagNode.attributeCount, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.childCount=" + mFnDagNode.childCount, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.dormantColor=" + mFnDagNode.dormantColor, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.hasUniqueName=" + mFnDagNode.hasUniqueName, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.inUnderWorld=" + mFnDagNode.inUnderWorld, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.isDefaultNode=" + mFnDagNode.isDefaultNode, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.isInstanceable=" + mFnDagNode.isInstanceable, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.isInstanced(true)=" + mFnDagNode.isInstanced(true), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.isInstanced(false)=" + mFnDagNode.isInstanced(false), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.isInstanced()=" + mFnDagNode.isInstanced(), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.instanceCount(true)=" + mFnDagNode.instanceCount(true), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.instanceCount(false)=" + mFnDagNode.instanceCount(false), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.isIntermediateObject=" + mFnDagNode.isIntermediateObject, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.isShared=" + mFnDagNode.isShared, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.objectColor=" + mFnDagNode.objectColor, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.parentCount=" + mFnDagNode.parentCount, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.parentNamespace=" + mFnDagNode.parentNamespace, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.uuid().asString()=" + mFnDagNode.uuid().asString(), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.dagRoot().apiType=" + mFnDagNode.dagRoot().apiType, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.model.equalEqual(mFnDagNode.objectProperty)=" + mFnDagNode.model.equalEqual(mFnDagNode.objectProperty), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.transformationMatrix.toString()=" + mFnDagNode.transformationMatrix.toString(), 3);
           };

            Action<MFnMesh> printMFnMesh = (MFnMesh _mFnMesh) =>
            {
                printMFnDagNode(mFnMesh);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numVertices=" + _mFnMesh.numVertices, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numEdges=" + _mFnMesh.numEdges, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numPolygons=" + _mFnMesh.numPolygons, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numFaceVertices=" + _mFnMesh.numFaceVertices, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numNormals=" + _mFnMesh.numNormals, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numUVSets=" + _mFnMesh.numUVSets, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numUVsProperty=" + _mFnMesh.numUVsProperty, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.displayColors=" + _mFnMesh.displayColors, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numColorSets=" + _mFnMesh.numColorSets, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numColorsProperty=" + _mFnMesh.numColorsProperty, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.currentUVSetName()=" + _mFnMesh.currentUVSetName(), 3);

                var _uvSetNames = new MStringArray();
                mFnMesh.getUVSetNames(_uvSetNames);
                foreach (var uvSetName in _uvSetNames)
                {
                    RaiseVerbose("BabylonExporter.Mesh | uvSetName=" + uvSetName, 3);
                    RaiseVerbose("BabylonExporter.Mesh | mFnMesh.numUVs(uvSetName)=" + mFnMesh.numUVs(uvSetName), 4);
                    MFloatArray us = new MFloatArray();
                    MFloatArray vs = new MFloatArray();
                    mFnMesh.getUVs(us, vs, uvSetName);
                    RaiseVerbose("BabylonExporter.Mesh | us.Count=" + us.Count, 4);
                }
            };

            Action<MFnTransform> printMFnTransform = (MFnTransform _mFnMesh) =>
            {
                printMFnDagNode(mFnMesh);
            };

            RaiseVerbose("BabylonExporter.Mesh | mFnMesh data", 2);
            printMFnMesh(mFnMesh);

            RaiseVerbose("BabylonExporter.Mesh | mFnTransform data", 2);
            printMFnTransform(mFnTransform);

            Print(mFnTransform, 2, "Print ExportMesh mFnTransform");

            Print(mFnMesh, 2, "Print ExportMesh mFnMesh");

            //// Geometry
            //MIntArray triangleCounts = new MIntArray();
            //MIntArray trianglesVertices = new MIntArray();
            //mFnMesh.getTriangles(triangleCounts, trianglesVertices);
            //RaiseVerbose("BabylonExporter.Mesh | triangleCounts.ToArray()=" + triangleCounts.ToArray().toString(), 3);
            //RaiseVerbose("BabylonExporter.Mesh | trianglesVertices.ToArray()=" + trianglesVertices.ToArray().toString(), 3);
            //int[] polygonsVertexCount = new int[mFnMesh.numPolygons];
            //for (int polygonId = 0; polygonId < mFnMesh.numPolygons; polygonId++)
            //{
            //    polygonsVertexCount[polygonId] = mFnMesh.polygonVertexCount(polygonId);
            //}
            //RaiseVerbose("BabylonExporter.Mesh | polygonsVertexCount=" + polygonsVertexCount.toString(), 3);

            ////MFloatPointArray points = new MFloatPointArray();
            ////mFnMesh.getPoints(points);
            ////RaiseVerbose("BabylonExporter.Mesh | points.ToArray()=" + points.ToArray().Select(mFloatPoint => mFloatPoint.toString()), 3);

            ////MFloatVectorArray normals = new MFloatVectorArray();
            ////mFnMesh.getNormals(normals);
            ////RaiseVerbose("BabylonExporter.Mesh | normals.ToArray()=" + normals.ToArray().Select(mFloatPoint => mFloatPoint.toString()), 3);

            //for (int polygonId = 0; polygonId < mFnMesh.numPolygons; polygonId++)
            //{
            //    MIntArray verticesId = new MIntArray();
            //    RaiseVerbose("BabylonExporter.Mesh | polygonId=" + polygonId, 3);

            //    int nbTriangles = triangleCounts[polygonId];
            //    RaiseVerbose("BabylonExporter.Mesh | nbTriangles=" + nbTriangles, 3);

            //    for (int triangleIndex = 0; triangleIndex < triangleCounts[polygonId]; triangleIndex++)
            //    {
            //        RaiseVerbose("BabylonExporter.Mesh | triangleIndex=" + triangleIndex, 3);
            //        int[] triangleVertices = new int[3];
            //        mFnMesh.getPolygonTriangleVertices(polygonId, triangleIndex, triangleVertices);
            //        RaiseVerbose("BabylonExporter.Mesh | triangleVertices=" + triangleVertices.toString(), 3);

            //        foreach (int vertexId in triangleVertices)
            //        {
            //            RaiseVerbose("BabylonExporter.Mesh | vertexId=" + vertexId, 3);
            //            MPoint point = new MPoint();
            //            mFnMesh.getPoint(vertexId, point);
            //            RaiseVerbose("BabylonExporter.Mesh | point=" + point.toString(), 3);

            //            MVector normal = new MVector();
            //            mFnMesh.getFaceVertexNormal(polygonId, vertexId, normal);
            //            RaiseVerbose("BabylonExporter.Mesh | normal=" + normal.toString(), 3);
            //        }
            //    }
            //}

            #endregion

            if (IsMeshExportable(mFnMesh, mDagPath) == false)
            {
                return null;
            }

            var babylonMesh = new BabylonMesh{
                                        name = mFnTransform.name,
                                        id = mFnTransform.uuid().asString(),
                                        visibility = Loader.GetVisibility(mFnTransform.fullPathName)
                                    };
            
            // Instance
            // For a mesh with instances, we distinguish between master and instance meshes:
            //      - a master mesh stores all the info of the mesh (transform, hierarchy, animations + vertices, indices, materials, bones...)
            //      - an instance mesh only stores the info of the node (transform, hierarchy, animations)

            // Check if this mesh has already been exported as a master mesh
            BabylonMesh babylonMasterMesh = GetMasterMesh(mFnMesh, babylonMesh);
            if (babylonMasterMesh != null)
            {
                RaiseMessage($"The master mesh {babylonMasterMesh.name} was already exported. This one will be exported as an instance.",2);

                // Export this node as instance
                var babylonInstanceMesh = new BabylonAbstractMesh { name = mFnTransform.name, id = mFnTransform.uuid().asString() };

                //// Add instance to master mesh
                List<BabylonAbstractMesh> instances = babylonMasterMesh.instances != null ? babylonMasterMesh.instances.ToList() : new List<BabylonAbstractMesh>();
                instances.Add(babylonInstanceMesh);
                babylonMasterMesh.instances = instances.ToArray();

                // Export transform / hierarchy / animations
                ExportNode(babylonInstanceMesh, mFnTransform, babylonScene);

                // Animations
                ExportNodeAnimation(babylonInstanceMesh, mFnTransform);

                return babylonInstanceMesh;
            }

            // Position / rotation / scaling / hierarchy
            ExportNode(babylonMesh, mFnTransform, babylonScene);

            // Misc.
            // TODO - Retreive from Maya
            //babylonMesh.receiveShadows = meshNode.MaxNode.RcvShadows == 1;
            //babylonMesh.applyFog = meshNode.MaxNode.ApplyAtmospherics == 1;

            if (mFnMesh.numPolygons < 1)
            {
                RaiseError($"Mesh {babylonMesh.name} has no face", 2);
            }

            if (mFnMesh.numVertices < 3)
            {
                RaiseError($"Mesh {babylonMesh.name} has not enough vertices", 2);
            }

            if (mFnMesh.numVertices >= 65536)
            {
                RaiseWarning($"Mesh {babylonMesh.name} has more than 65536 vertices which means that it will require specific WebGL extension to be rendered. This may impact portability of your scene on low end devices.", 2);
            }

            // Animations
            ExportNodeAnimation(babylonMesh, mFnTransform);

            // Material
            MObjectArray shaders = new MObjectArray();
            mFnMesh.getConnectedShaders(0, shaders, new MIntArray());
            if (shaders.Count > 0)
            {
                List<MFnDependencyNode> materials = new List<MFnDependencyNode>();
                foreach (MObject shader in shaders)
                {
                    // Retreive material
                    MFnDependencyNode shadingEngine = new MFnDependencyNode(shader);
                    MPlug mPlugSurfaceShader = shadingEngine.findPlug("surfaceShader");
                    MObject materialObject = mPlugSurfaceShader.source.node;
                    MFnDependencyNode material = new MFnDependencyNode(materialObject);

                    materials.Add(material);
                }

                if (shaders.Count == 1)
                {
                    MFnDependencyNode material = materials[0];

                    // Material is referenced by id
                    babylonMesh.materialId = material.uuid().asString();

                    // Register material for export if not already done
                    if (!referencedMaterials.Contains(material, new MFnDependencyNodeEqualityComparer()))
                    {
                        referencedMaterials.Add(material);
                    }
                }
                else
                {
                    // Create a new id for the group of sub materials
                    string uuidMultiMaterial = GetMultimaterialUUID(materials);

                    // Multi material is referenced by id
                    babylonMesh.materialId = uuidMultiMaterial;

                    // Register multi material for export if not already done
                    if (!multiMaterials.ContainsKey(uuidMultiMaterial))
                    {
                        multiMaterials.Add(uuidMultiMaterial, materials);
                    }
                }
            }

            var vertices = new List<GlobalVertex>();
            var indices = new List<int>();

            var uvSetNames = new MStringArray();
            mFnMesh.getUVSetNames(uvSetNames);
            bool[] isUVExportSuccess = new bool[Math.Min(uvSetNames.Count, 2)];
            for (int indexUVSet = 0; indexUVSet < isUVExportSuccess.Length; indexUVSet++)
            {
                isUVExportSuccess[indexUVSet] = true;
            }

            // skin
            if(_exportSkin)
            {
                mFnSkinCluster = getMFnSkinCluster(mFnMesh);
            }
            int maxNbBones = 0;
            if (mFnSkinCluster != null)
            {
                RaiseMessage($"mFnSkinCluster.name | {mFnSkinCluster.name}", 2);
                Print(mFnSkinCluster, 3, $"Print {mFnSkinCluster.name}");

                // Get the bones dictionary<name, index> => it represents all the bones in the skeleton
                indexByNodeName = GetIndexByFullPathNameDictionary(mFnSkinCluster);

                // Get the joint names that influence this mesh
                allMayaInfluenceNames = GetBoneFullPathName(mFnSkinCluster, mFnTransform);

                // Get the max number of joints acting on a vertex
                int maxNumInfluences = GetMaxInfluence(mFnSkinCluster, mFnTransform, mFnMesh);

                RaiseMessage($"Max influences : {maxNumInfluences}",2);
                if (maxNumInfluences > 8)
                {
                    RaiseWarning($"Too many bones influences per vertex: {maxNumInfluences}. Babylon.js only support up to 8 bones influences per vertex.", 2);
                    RaiseWarning("The result may not be as expected.",2);
                }
                maxNbBones = Math.Min(maxNumInfluences, 8);

                if (indexByNodeName != null && allMayaInfluenceNames != null)
                {
                    babylonMesh.skeletonId = GetSkeletonIndex(mFnSkinCluster);
                }
                else
                {
                    mFnSkinCluster = null;
                }
            }
            // Export tangents if option is checked and mesh have tangents
            bool isTangentExportSuccess = _exportTangents;

            // TODO - color, alpha
            //var hasColor = unskinnedMesh.NumberOfColorVerts > 0;
            //var hasAlpha = unskinnedMesh.GetNumberOfMapVerts(-2) > 0;

            // TODO - Add custom properties
            //var optimizeVertices = false; // meshNode.MaxNode.GetBoolProperty("babylonjs_optimizevertices");
            var optimizeVertices = _optimizeVertices; // global option

            // Compute normals
            var subMeshes = new List<BabylonSubMesh>();

            if (hasMorphTarget)
            {
                ExtractGeometry(meshShapeOrig, vertices, indices, subMeshes, uvSetNames, ref isUVExportSuccess, ref isTangentExportSuccess, optimizeVertices);
            }
            else
            {
                ExtractGeometry(mFnMesh, vertices, indices, subMeshes, uvSetNames, ref isUVExportSuccess, ref isTangentExportSuccess, optimizeVertices);
            }

            if (vertices.Count >= 65536)
            {
                RaiseWarning($"Mesh {babylonMesh.name} has {vertices.Count} vertices. This may prevent your scene to work on low end devices where 32 bits indice are not supported", 2);

                if (!optimizeVertices)
                {
                    RaiseError("You can try to optimize your object using [Try to optimize vertices] option", 2);
                }
            }

            for (int indexUVSet = 0; indexUVSet < isUVExportSuccess.Length; indexUVSet++)
            {
                string uvSetName = uvSetNames[indexUVSet];
                // If at least one vertex is mapped to an UV coordinate but some have failed to be exported
                if (isUVExportSuccess[indexUVSet] == false && mFnMesh.numUVs(uvSetName) > 0)
                {
                    RaiseWarning($"Failed to export UV set named {uvSetName}. Ensure all vertices are mapped to a UV coordinate.", 2);
                }
            }

            RaiseMessage($"{vertices.Count} vertices, {indices.Count / 3} faces", 2);

            // Buffers
            babylonMesh.positions = vertices.SelectMany(v => v.Position).ToArray();
            babylonMesh.normals = vertices.SelectMany(v => v.Normal).ToArray();

            // export the skin
            if (mFnSkinCluster != null)
            {
                babylonMesh.matricesWeights = vertices.SelectMany(v => v.Weights.ToArray()).ToArray();
                babylonMesh.matricesIndices = vertices.Select(v => v.BonesIndices).ToArray();

                babylonMesh.numBoneInfluencers = maxNbBones;
                if (maxNbBones > 4)
                {
                    babylonMesh.matricesWeightsExtra = vertices.SelectMany(v => v.WeightsExtra != null ? v.WeightsExtra.ToArray() : new[] { 0.0f, 0.0f, 0.0f, 0.0f }).ToArray();
                    babylonMesh.matricesIndicesExtra = vertices.Select(v => v.BonesIndicesExtra).ToArray();
                }
            }

            // Tangent
            if (isTangentExportSuccess)
            {
                babylonMesh.tangents = vertices.SelectMany(v => v.Tangent).ToArray();
            }
            // Color
            string colorSetName;
            mFnMesh.getCurrentColorSetName(out colorSetName);
            if (mFnMesh.numColors(colorSetName) > 0) {
                babylonMesh.colors = vertices.SelectMany(v => v.Color).ToArray();
            }
            // UVs
            if (uvSetNames.Count > 0 && isUVExportSuccess[0])
            {
                
                babylonMesh.uvs = vertices.SelectMany(v => v.UV).ToArray();
            }
            if (uvSetNames.Count > 1 && isUVExportSuccess[1])
            {
                babylonMesh.uvs2 = vertices.SelectMany(v => v.UV2).ToArray();
            }

            babylonMesh.subMeshes = subMeshes.ToArray();

            // Buffers - Indices
            babylonMesh.indices = indices.ToArray();


            // ------------------------
            // ---- Morph targets -----
            // ------------------------
            if (hasMorphTarget)
            {
                // Maya blend shape influencing the mesh
                RaiseMessage("Morph target", 2);
                IList<MFnBlendShapeDeformer> blendShapeDeformers = GetBlendShape(mFnMesh.objectProperty);
            
                if(blendShapeDeformers.Count > 7)
                {
                    RaiseWarning($"There are {blendShapeDeformers.Count} morph targets.", 3);
                    RaiseWarning($"Please be aware that most of the browsers are limited to 16 attributes per mesh. Adding a single morph target to a mesh add 2 new attributes (position + normal). This could quickly go beyond the max attributes limitation.", 3);
                }

                // Morph Target Manager
                BabylonMorphTargetManager babylonMorphTargetManager = new BabylonMorphTargetManager();
                babylonScene.MorphTargetManagersList.Add(babylonMorphTargetManager);
                babylonMesh.morphTargetManagerId = babylonMorphTargetManager.id;

                IList<BabylonMorphTarget> babylonMorphTargets = GetMorphTargets(mFnMesh.objectProperty);
                babylonMorphTargetManager.targets = babylonMorphTargets.ToArray();
            }



            babylonScene.MeshesList.Add(babylonMesh);
            RaiseMessage("BabylonExporter.Mesh | done", 2);

            return babylonMesh;
        }

        /// <summary>
        /// Extract ordered indices on a triangle basis
        /// Extract position and normal of each vertex per face
        /// </summary>
        /// <param name="mFnMesh"></param>
        /// <param name="vertices"></param>
        /// <param name="indices"></param>
        /// <param name="subMeshes"></param>
        /// <param name="uvSetNames"></param>
        /// <param name="isUVExportSuccess"></param>
        /// <param name="optimizeVertices"></param>
        private void ExtractGeometry(MFnMesh mFnMesh, List<GlobalVertex> vertices, List<int> indices, List<BabylonSubMesh> subMeshes, MStringArray uvSetNames, ref bool[] isUVExportSuccess, ref bool isTangentExportSuccess, bool optimizeVertices)
        {
            List<GlobalVertex>[] verticesAlreadyExported = null;

            if (optimizeVertices)
            {
                verticesAlreadyExported = new List<GlobalVertex>[mFnMesh.numVertices];
            }

            MIntArray triangleCounts = new MIntArray();
            MIntArray trianglesVertices = new MIntArray();
            mFnMesh.getTriangles(triangleCounts, trianglesVertices);
            
            MObjectArray shaders = new MObjectArray();
            MIntArray faceMatIndices = new MIntArray(); // given a face index => get a shader index
            mFnMesh.getConnectedShaders(0, shaders, faceMatIndices);

            // Export geometry even if an error occured with shaders
            int nbShaders = Math.Max(1, shaders.Count);
            bool checkShader = nbShaders == shaders.Count;
            RaiseVerbose("shaders.Count=" + shaders.Count, 2);

            // For each material of this mesh
            for (int indexShader = 0; indexShader < nbShaders; indexShader++)
            {
                var nbIndicesSubMesh = 0;
                var minVertexIndexSubMesh = int.MaxValue;
                var maxVertexIndexSubMesh = int.MinValue;
                var subMesh = new BabylonSubMesh { indexStart = indices.Count, materialIndex = indexShader };
                
                // For each polygon of this mesh
                for (int polygonId = 0; polygonId < faceMatIndices.Count; polygonId++)
                {
                    if (checkShader && faceMatIndices[polygonId] != indexShader)
                    {
                        continue;
                    }

                    // The object-relative (mesh-relative/global) vertex indices for this face
                    MIntArray polygonVertices = new MIntArray();
                    mFnMesh.getPolygonVertices(polygonId, polygonVertices);

                    // For each triangle of this polygon
                    for (int triangleId = 0; triangleId < triangleCounts[polygonId]; triangleId++)
                    {
                        int[] polygonTriangleVertices = new int[3];
                        mFnMesh.getPolygonTriangleVertices(polygonId, triangleId, polygonTriangleVertices);

                        /*
                         * Switch coordinate system at global level
                         * 
                         * Piece of code kept just in case
                         * See BabylonExporter for more information
                         */
                        //// Inverse winding order to flip faces
                        //var tmp = triangleVertices[1];
                        //triangleVertices[1] = triangleVertices[2];
                        //triangleVertices[2] = tmp;

                        // For each vertex of this triangle (3 vertices per triangle)
                        foreach (int vertexIndexGlobal in polygonTriangleVertices)
                        {
                            // Get the face-relative (local) vertex id
                            int vertexIndexLocal = 0;
                            for (vertexIndexLocal = 0; vertexIndexLocal < polygonVertices.Count - 1; vertexIndexLocal++) // -1 to stop at vertexIndexLocal=2
                            {
                                if (polygonVertices[vertexIndexLocal] == vertexIndexGlobal)
                                {
                                    break;
                                }
                            }

                            GlobalVertex vertex = ExtractVertex(mFnMesh, polygonId, vertexIndexGlobal, vertexIndexLocal, uvSetNames, ref isUVExportSuccess, ref isTangentExportSuccess);

                            // Optimize vertices
                            if (verticesAlreadyExported != null)
                            {
                                if (verticesAlreadyExported[vertexIndexGlobal] != null)
                                {
                                    var index = verticesAlreadyExported[vertexIndexGlobal].IndexOf(vertex);

                                    // If a stored vertex is similar to current vertex
                                    if (index > -1)
                                    {
                                        // Use stored vertex instead of current one
                                        vertex = verticesAlreadyExported[vertexIndexGlobal][index];
                                    }
                                    else
                                    {
                                        vertex.CurrentIndex = vertices.Count;
                                        verticesAlreadyExported[vertexIndexGlobal].Add(vertex);
                                        vertices.Add(vertex);
                                    }
                                }
                                else
                                {
                                    verticesAlreadyExported[vertexIndexGlobal] = new List<GlobalVertex>();

                                    vertex.CurrentIndex = vertices.Count;
                                    verticesAlreadyExported[vertexIndexGlobal].Add(vertex);
                                    vertices.Add(vertex);
                                }
                            }
                            else
                            {
                                vertex.CurrentIndex = vertices.Count;
                                vertices.Add(vertex);
                            }

                            indices.Add(vertex.CurrentIndex);

                            minVertexIndexSubMesh = Math.Min(minVertexIndexSubMesh, vertex.CurrentIndex);
                            maxVertexIndexSubMesh = Math.Max(maxVertexIndexSubMesh, vertex.CurrentIndex);
                            nbIndicesSubMesh++;
                        }
                    }
                }

                if (nbIndicesSubMesh != 0)
                {
                    subMesh.indexCount = nbIndicesSubMesh;
                    subMesh.verticesStart = minVertexIndexSubMesh;
                    subMesh.verticesCount = maxVertexIndexSubMesh - minVertexIndexSubMesh + 1;

                    subMeshes.Add(subMesh);
                }
            }
        }

        /// <summary>
        /// Extract geometry (position, normal, UVs...) for a specific vertex
        /// </summary>
        /// <param name="mFnMesh"></param>
        /// <param name="polygonId">The polygon (face) to examine</param>
        /// <param name="vertexIndexGlobal">The object-relative (mesh-relative/global) vertex index</param>
        /// <param name="vertexIndexLocal">The face-relative (local) vertex id to examine</param>
        /// <param name="uvSetNames"></param>
        /// <param name="isUVExportSuccess"></param>
        /// <returns></returns>
        private GlobalVertex ExtractVertex(MFnMesh mFnMesh, int polygonId, int vertexIndexGlobal, int vertexIndexLocal, MStringArray uvSetNames, ref bool[] isUVExportSuccess, ref bool isTangentExportSuccess)
        {
            MPoint point = new MPoint();
            mFnMesh.getPoint(vertexIndexGlobal, point);

            MVector normal = new MVector();
            mFnMesh.getFaceVertexNormal(polygonId, vertexIndexGlobal, normal);

            // Switch coordinate system at object level
            point.z *= -1;
            normal.z *= -1;

            var vertex = new GlobalVertex
            {
                BaseIndex = vertexIndexGlobal,
                Position = point.toArray(),
                Normal = normal.toArray()
            };

            // Tangent
            if (isTangentExportSuccess)
            {
                try
                {
                    MVector tangent = new MVector();
                    mFnMesh.getFaceVertexTangent(polygonId, vertexIndexGlobal, tangent);

                    // Switch coordinate system at object level
                    tangent.z *= -1;

                    int tangentId = mFnMesh.getTangentId(polygonId, vertexIndexGlobal);
                    bool isRightHandedTangent = mFnMesh.isRightHandedTangent(tangentId);

                    // Invert W to switch to left handed system
                    vertex.Tangent = new float[] { (float)tangent.x, (float)tangent.y, (float)tangent.z, isRightHandedTangent ? -1 : 1 };
                }
                catch
                {
                    // Exception raised when mesh don't have tangents
                    isTangentExportSuccess = false;
                }
            }

            // Color
            int colorIndex;
            string colorSetName;
            float[] defaultColor = new float[] { 1, 1, 1, 0 };
            MColor color = new MColor();
            mFnMesh.getCurrentColorSetName(out colorSetName);

            if (mFnMesh.numColors(colorSetName) > 0)
            {
                //Get the color index
                mFnMesh.getColorIndex(polygonId, vertexIndexLocal, out colorIndex);
                
                //if a color is set
                if (colorIndex != -1)
                {
                    mFnMesh.getColor(colorIndex, color);
                    vertex.Color = color.toArray();
                }
                //else set the default color
                else
                {
                    vertex.Color = defaultColor;
                }
            }

            // UV
            int indexUVSet = 0;
            if (uvSetNames.Count > indexUVSet && isUVExportSuccess[indexUVSet])
            {
                try
                {
                    float u = 0, v = 0;
                    mFnMesh.getPolygonUV(polygonId, vertexIndexLocal, ref u, ref v, uvSetNames[indexUVSet]);
                    vertex.UV = new float[] { u, v };
                }
                catch
                {
                    // An exception is raised when a vertex isn't mapped to an UV coordinate
                    isUVExportSuccess[indexUVSet] = false;
                }
            }
            indexUVSet = 1;
            if (uvSetNames.Count > indexUVSet && isUVExportSuccess[indexUVSet])
            {
                try
                {
                    float u = 0, v = 0;
                    mFnMesh.getPolygonUV(polygonId, vertexIndexLocal, ref u, ref v, uvSetNames[indexUVSet]);
                    vertex.UV2 = new float[] { u, v };
                }
                catch
                {
                    // An exception is raised when a vertex isn't mapped to an UV coordinate
                    isUVExportSuccess[indexUVSet] = false;
                }
            }

            // skin
            if (mFnSkinCluster != null)
            {
                // Filter null weights
                Dictionary<int, double> weightByInfluenceIndex = new Dictionary<int, double>(); // contains the influence indices with weight > 0

                // Export Weights and BonesIndices for the vertex
                // Get the weight values of all the influences for this vertex
                allMayaInfluenceWeights = new MDoubleArray();
                MGlobal.executeCommand($"skinPercent -query -value {mFnSkinCluster.name} {mFnTransform.name}.vtx[{vertexIndexGlobal}]",
                                        allMayaInfluenceWeights);
                allMayaInfluenceWeights.get(out double[] allInfluenceWeights);

                for (int influenceIndex = 0; influenceIndex < allInfluenceWeights.Length; influenceIndex++)
                {
                    double weight = allInfluenceWeights[influenceIndex];

                    if (weight > 0)
                    {
                        try
                        {
                            // add indice and weight in the local lists
                            weightByInfluenceIndex.Add(indexByNodeName[allMayaInfluenceNames[influenceIndex]], weight);
                        }
                        catch (Exception e)
                        {
                            RaiseError(e.Message, 2);
                            RaiseError(e.StackTrace, 3);
                        }
                    }
                }

                // normalize weights => Sum weights == 1
                Normalize(ref weightByInfluenceIndex);

                // decreasing sort
                OrderByDescending(ref weightByInfluenceIndex);

                int bonesCount = indexByNodeName.Count; // number total of bones/influences for the mesh
                float weight0 = 0;
                float weight1 = 0;
                float weight2 = 0;
                float weight3 = 0;
                int bone0 = bonesCount;
                int bone1 = bonesCount;
                int bone2 = bonesCount;
                int bone3 = bonesCount;
                int nbBones = weightByInfluenceIndex.Count; // number of bones/influences for this vertex

                if (nbBones == 0)
                {
                    weight0 = 1.0f;
                    bone0 = bonesCount;
                }

                if (nbBones > 0)
                {
                    bone0 = weightByInfluenceIndex.ElementAt(0).Key;
                    weight0 = (float)weightByInfluenceIndex.ElementAt(0).Value;

                    if (nbBones > 1)
                    {
                        bone1 = weightByInfluenceIndex.ElementAt(1).Key;
                        weight1 = (float)weightByInfluenceIndex.ElementAt(1).Value;

                        if (nbBones > 2)
                        {
                            bone2 = weightByInfluenceIndex.ElementAt(2).Key;
                            weight2 = (float)weightByInfluenceIndex.ElementAt(2).Value;

                            if (nbBones > 3)
                            {
                                bone3 = weightByInfluenceIndex.ElementAt(3).Key;
                                weight3 = (float)weightByInfluenceIndex.ElementAt(3).Value;
                            }
                        }
                    }
                }

                float[] weights = { weight0, weight1, weight2, weight3 };
                vertex.Weights = weights;
                vertex.BonesIndices = (bone3 << 24) | (bone2 << 16) | (bone1 << 8) | bone0;

                if (nbBones > 4)
                {
                    bone0 = weightByInfluenceIndex.ElementAt(4).Key;
                    weight0 = (float)weightByInfluenceIndex.ElementAt(4).Value;
                    weight1 = 0;
                    weight2 = 0;
                    weight3 = 0;

                    if (nbBones > 5)
                    {
                        bone1 = weightByInfluenceIndex.ElementAt(5).Key;
                        weight1 = (float)weightByInfluenceIndex.ElementAt(4).Value;

                        if (nbBones > 6)
                        {
                            bone2 = weightByInfluenceIndex.ElementAt(6).Key;
                            weight2 = (float)weightByInfluenceIndex.ElementAt(4).Value;

                            if (nbBones > 7)
                            {
                                bone3 = weightByInfluenceIndex.ElementAt(7).Key;
                                weight3 = (float)weightByInfluenceIndex.ElementAt(7).Value;
                            }
                        }
                    }

                    float[] weightsExtra = { weight0, weight1, weight2, weight3 };
                    vertex.WeightsExtra = weightsExtra;
                    vertex.BonesIndicesExtra = (bone3 << 24) | (bone2 << 16) | (bone1 << 8) | bone0;
                }
            }
            return vertex;
        }
        
        private void ExportNode(BabylonAbstractMesh babylonAbstractMesh, MFnTransform mFnTransform, BabylonScene babylonScene)
        {
            RaiseVerbose("BabylonExporter.Mesh | ExportNode", 2);

            // Position / rotation / scaling
            ExportTransform(babylonAbstractMesh, mFnTransform);

            // Hierarchy
            ExportHierarchy(babylonAbstractMesh, mFnTransform);
        }

        private void ExportTransform(BabylonAbstractMesh babylonAbstractMesh, MFnTransform mFnTransform)
        {
            // Position / rotation / scaling
            RaiseVerbose("BabylonExporter.Mesh | ExportTransform", 2);
            float[] position = null;
            float[] rotationQuaternion = null;
            float[] rotation = null;
            float[] scaling = null;
            GetTransform(mFnTransform, ref position, ref rotationQuaternion, ref rotation, ref scaling);

            babylonAbstractMesh.position = position;
            if (_exportQuaternionsInsteadOfEulers)
            {
                babylonAbstractMesh.rotationQuaternion = rotationQuaternion;
            }
            else
            {
                babylonAbstractMesh.rotation = rotation;
            }
            babylonAbstractMesh.scaling = scaling;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mFnDagNode">DAG function set of the node (mesh) below the transform</param>
        /// <param name="mDagPath">DAG path of the transform above the node</param>
        /// <returns></returns>
        private bool IsMeshExportable(MFnDagNode mFnDagNode, MDagPath mDagPath)
        {
            return IsNodeExportable(mFnDagNode, mDagPath);
        }

        private MFnSkinCluster getMFnSkinCluster(MFnMesh mFnMesh)
        {
            MFnSkinCluster mFnSkinCluster = null;

            MPlugArray connections = new MPlugArray();
            mFnMesh.getConnections(connections);
            foreach (MPlug connection in connections)
            {
                MObject source = connection.source.node;
                if (source != null)
                {
                    if (source.hasFn(MFn.Type.kSkinClusterFilter))
                    {
                        mFnSkinCluster = new MFnSkinCluster(source);
                    }

                    if ((mFnSkinCluster == null) && (source.hasFn(MFn.Type.kSet) || source.hasFn(MFn.Type.kPolyNormalPerVertex)))
                    {
                        mFnSkinCluster = getMFnSkinCluster(source);
                    }
                }
            }

            return mFnSkinCluster;
        }

        private MFnSkinCluster getMFnSkinCluster(MObject mObject)
        {
            MFnSkinCluster mFnSkinCluster = null;

            MFnDependencyNode mFnDependencyNode = new MFnDependencyNode(mObject);
            MPlugArray connections = new MPlugArray();
            mFnDependencyNode.getConnections(connections);
            for (int index = 0; index < connections.Count && mFnSkinCluster == null; index++)
            {
                MObject source = connections[index].source.node;
                if (source != null && source.hasFn(MFn.Type.kSkinClusterFilter))
                {
                    mFnSkinCluster = new MFnSkinCluster(source);
                }
            }

            return mFnSkinCluster;
        }


        /// <summary>
        /// Instances manager
        /// </summary>
        private List<MFnMesh> exportedMFnMesh = new List<MFnMesh>();
        private List<BabylonMesh> exportedMasterBabylonMesh = new List<BabylonMesh>();
        private BabylonMesh GetMasterMesh(MFnMesh mFnMesh, BabylonMesh babylonMesh)
        {
            BabylonMesh babylonMasterMesh = null;
            int index = exportedMFnMesh.FindIndex(mesh => mesh.fullPathName.Equals(mFnMesh.fullPathName));

            if(index == -1)
            {
                exportedMFnMesh.Add(mFnMesh);
                exportedMasterBabylonMesh.Add(babylonMesh);
            }
            else
            {
                babylonMasterMesh = exportedMasterBabylonMesh[index];
            }

            return babylonMasterMesh;
        }


        /// <summary>
        /// Check if a Maya object is link to a blend shape by counting its connections to it. 
        /// </summary>
        /// <param name="mObject"></param>
        /// <returns>
        /// True, if there at least one connection to a blend shape
        /// False, otherwise
        /// </returns>
        private bool hasBlendShape(MObject mObject)
        {
            IList<MFnBlendShapeDeformer> blendShapeDeformers = GetBlendShape(mObject);
            return blendShapeDeformers.Count > 0;
        }


        /// <summary>
        /// Search the blend shapes through the connections of the Maya object
        /// </summary>
        /// <param name="mObject"></param>
        /// <returns>A list with all blend shape linked to the object</returns>
        private IDictionary<MObject, IList<MFnBlendShapeDeformer>> blendShapeByMObject = new Dictionary<MObject, IList<MFnBlendShapeDeformer>>();
        private IList<MFnBlendShapeDeformer> GetBlendShape(MObject mObject)
        {
            var pair =  blendShapeByMObject.FirstOrDefault(item => item.Key.equalEqual(mObject));
            if (! pair.Equals(default(KeyValuePair<MObject, IList<MFnBlendShapeDeformer>>)))
            {
                return pair.Value;
            }

            IList<MFnBlendShapeDeformer> blendShapeDeformers = GetBlendShapeSub(mObject);
            
            // uniqueness
            IList <MFnBlendShapeDeformer> uniqBlendShapeDeformers = new List<MFnBlendShapeDeformer>();
            for (int index = 0; index < blendShapeDeformers.Count; index++)
            {
                MFnBlendShapeDeformer blendShapeDeformer = blendShapeDeformers[index];

                if (uniqBlendShapeDeformers.Count(item => item.name.Equals(blendShapeDeformer.name)) == 0)
                {
                    RaiseMessage("Blend shape: " + blendShapeDeformer.name, 3);
                    uniqBlendShapeDeformers.Add(blendShapeDeformer);
                }
            }

            blendShapeByMObject[mObject] = uniqBlendShapeDeformers;
            return uniqBlendShapeDeformers;
        }


        private IList<MFnBlendShapeDeformer> GetBlendShapeSub(MObject mObject)
        {
            List<MFnBlendShapeDeformer> blendShapeDeformers = new List<MFnBlendShapeDeformer>();

            MFnDependencyNode dependencyNode = new MFnDependencyNode(mObject);
            MPlugArray connections = new MPlugArray();
            dependencyNode.getConnections(connections);
            foreach (MPlug connection in connections)
            {
                MObject source = connection.source.node;
                if (source != null)
                {
                    if (source.hasFn(MFn.Type.kSet))
                    {
                        blendShapeDeformers.AddRange(GetBlendShapeSub(source));
                    }

                    if (source.hasFn(MFn.Type.kBlendShape))
                    {
                        MFnBlendShapeDeformer blendShapeDeformer = new MFnBlendShapeDeformer(source);
                        blendShapeDeformers.Add(blendShapeDeformer);
                    }
                }
            }

            return blendShapeDeformers;
        }



        /// <summary>
        /// Convert a Maya blendShape influencing a Maya object into a BabylonMorphTarget list
        /// </summary>
        /// <param name="baseObject">The Maya object influenced by the blendShapes</param>
        /// <param name="blendShapeDeformers">List of Maya blendShape. Use GetBlendShape function to get the right one.</param>
        /// <returns>BabylonMorphTarget list</returns>
        private IList<BabylonMorphTarget> GetMorphTargets(MObject baseObject)
        {
            // Morph Targets
            IList<MFnBlendShapeDeformer> blendShapeDeformers = GetBlendShape(baseObject);
            IList <BabylonMorphTarget> babylonMorphTargets = new List<BabylonMorphTarget>();

            for (int index = 0; index < blendShapeDeformers.Count; index++)
            {
                MFnBlendShapeDeformer blendShapeDeformer = blendShapeDeformers[index];

                float envelope = blendShapeDeformer.envelope;

                MIntArray weightIndexList = new MIntArray();    // list of weight. For each weith, there are multiple targets
                blendShapeDeformer.weightIndexList(weightIndexList);


                for (int i = 0; i < weightIndexList.Count; i++)
                {
                    int weightIndex = weightIndexList[i];
                    float weight = blendShapeDeformer.weight((uint)weightIndex);

                    MObjectArray targets = new MObjectArray();  // the targets for the given weight
                    blendShapeDeformer.getTargets(baseObject, weightIndex, targets);

                    for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                    {
                        MObject target = targets[targetIndex];

                        BabylonMorphTarget babylonMorphTarget = new BabylonMorphTarget
                        {
                            name = (new MFnDependencyNode(targets[targetIndex])).name,
                            influence = envelope * weight
                        };
                        babylonMorphTargets.Add(babylonMorphTarget);

                        // Target geometry
                        var targetVertices = new List<GlobalVertex>();
                        var indices = new List<int>();
                        var subMeshes = new List<BabylonSubMesh>();
                        var uvSetNames = new MStringArray();
                        bool[] isUVExportSuccess = { false, false };
                        bool isTangentExportSuccess = _exportTangents;
                        bool optimizeVertices = _optimizeVertices;
                        ExtractGeometry(new MFnMesh(target), targetVertices, indices, subMeshes, uvSetNames, ref isUVExportSuccess, ref isTangentExportSuccess, optimizeVertices);

                        babylonMorphTarget.positions = targetVertices.SelectMany(v => v.Position).ToArray();
                        babylonMorphTarget.normals = targetVertices.SelectMany(v => v.Normal).ToArray();

                        // Tangent
                        if (isTangentExportSuccess)
                        {
                            babylonMorphTarget.tangents = targetVertices.SelectMany(v => v.Tangent.Take(3)).ToArray();
                        }

                        // Animation
                        babylonMorphTarget.animations = GetAnimationsFrameByFrameInfluence(blendShapeDeformer.name, weightIndex).ToArray();
                    }

                }
            }

            return babylonMorphTargets;
        }


        private IDictionary<double, IList<double>> GetMorphWeightsByFrame(string blendShapeDeformerName)
        {
            Dictionary<double, IList<double>> weights = new Dictionary<double, IList<double>>();

            // Get the keyframe of the blendSape
            MDoubleArray keyArray = new MDoubleArray();
            MGlobal.executeCommand($"keyframe -t \":\" -q -timeChange {blendShapeDeformerName}", keyArray);

            SortedSet<double> sortedKeys = new SortedSet<double>(keyArray);
            List<double> keys = new List<double>(sortedKeys);

            for(int index = 0; index < keys.Count; index++)
            {
                // Get the weight at this keyframe
                double key = keys[index];
                MDoubleArray weightArray = new MDoubleArray();
                MGlobal.executeCommand($"getAttr -t {key} {blendShapeDeformerName}.weight", weightArray);

                weights[key] = weightArray;
            }

            return weights;
        }


        private IList<BabylonAnimation> GetAnimationsFrameByFrameInfluence(string blendShapeDeformerName, int weightIndex)
        {
            IList<BabylonAnimation> animations = new List<BabylonAnimation>();
            BabylonAnimation animation = null;

            IDictionary<double, IList<double>> morphWeights = GetMorphWeightsByFrame(blendShapeDeformerName);

            // get keys
            List<BabylonAnimationKey> keys = new List<BabylonAnimationKey>();
            for (int index = 0; index < morphWeights.Count; index++)
            {
                KeyValuePair<double, IList<double>> keyValue = morphWeights.ElementAt(index);
                // Set the animation key
                BabylonAnimationKey key = new BabylonAnimationKey()
                {
                    frame = (int)keyValue.Key,
                    values = new float[] { (float) keyValue.Value[weightIndex] }
                };

                keys.Add(key);
            }

            List<BabylonAnimationKey> keysFull = new List<BabylonAnimationKey>(keys);

            // Optimization
            OptimizeAnimations(keys, false); // Do not remove linear animation keys for bones

            // Ensure animation has at least 2 frames
            if (IsAnimationKeysRelevant(keys))
            {
                // Animations
                animation = new BabylonAnimation()
                {
                    name = "influence animation", // override default animation name
                    dataType = (int)BabylonAnimation.DataType.Float,
                    loopBehavior = (int)BabylonAnimation.LoopBehavior.Cycle,
                    framePerSecond = Loader.GetFPS(),
                    keys = keys.ToArray(),
                    keysFull = keysFull,
                    property = "influence"
                };

                animations.Add(animation);
            }

            return animations;
        }
    }
}
