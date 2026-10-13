using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record ProjectReadinessRow(
    string Area,
    string Status,
    bool GatePassed,
    long Observed,
    long Expected,
    string Evidence,
    string NextRequiredEvidence,
    string Notes);

public sealed record ProjectReadinessSummary(
    IReadOnlyList<ProjectReadinessRow> Rows,
    bool Ready,
    string IsoSha256,
    long IsoLength,
    FileAttributes IsoAttributes);

/// <summary>
/// Produces a conservative, evidence-backed readiness matrix.  A passing row
/// means that the corresponding verification gate is present; it does not
/// promote structural or candidate observations to original game semantics.
/// Rows with an <c>-authored-runtime</c> or <c>-candidate-runtime</c> suffix
/// are deliberately scoped runtime gates and do not relax the corresponding
/// original-semantics rows.
/// </summary>
public static class ProjectReadinessAudit
{
    private readonly record struct WindowedFrameCaptureEvidence(
        bool Valid,
        long ByteLength,
        string Header,
        bool NonUniform,
        string Sha256);

    private readonly record struct StaticRenderEvidence(
        bool Valid,
        int RenderXrefRows,
        int UniqueRenderLiterals,
        int RenderContextRows,
        int RenderContextWithExecutableCalls,
        int DistinctExecutableCallTargets,
        string CategoryCounts,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderTargetEvidence(
        bool Valid,
        int CandidateTargets,
        int DisassembledTargets,
        int RetTerminatedTargets,
        int PrologueLikeTargets,
        int DirectCallCount,
        int IndirectCallCount,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderCfgEvidence(
        bool Valid,
        int CandidateTargets,
        int AnalyzedTargets,
        int TargetsWithReachableCfg,
        int TargetsWithReachableRet,
        int TotalBasicBlocks,
        int TotalDirectCalls,
        int TotalIndirectControlTransfers,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderCallTargetEvidence(
        bool Valid,
        int SourceRenderTargetWindows,
        int SourceWindowsWithDirectCalls,
        int UniqueDirectCallTargets,
        int MappedExecutableTargets,
        int DisassembledTargets,
        int RetTerminatedTargets,
        int PrologueLikeTargets,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderCallGraphEvidence(
        bool Valid,
        int SeedTargets,
        int SeedMappedTargets,
        int MaxDepth,
        int DiscoveredNodes,
        int MappedExecutableNodes,
        int DisassembledNodes,
        int RetTerminatedNodes,
        int NodesWithDirectCalls,
        int UniqueEdges,
        int TotalEdgeOccurrences,
        bool TruncatedByMaxNodes,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectTransferEvidence(
        bool Valid,
        int CandidateTargets,
        int AnalyzedTargets,
        int TargetsWithIndirectTransfers,
        int TotalIndirectTransfers,
        int IndirectCalls,
        int IndirectJumps,
        int RegisterOperands,
        int MemoryOperands,
        int AbsoluteMemoryOperands,
        int DistinctOperandShapes,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectReferenceEvidence(
        bool Valid,
        int IndirectRows,
        int ReferenceRows,
        int AbsoluteMemoryRows,
        int IndexedTableBaseRows,
        int BoundedReferenceReads,
        int ReferenceWordsValid,
        int ReferenceWordsInExecutableSections,
        int DistinctReferenceAddresses,
        int DistinctRawWords,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectReferenceSliceEvidence(
        bool Valid,
        int ReferenceRows,
        int DistinctRawWords,
        int ExecutableRawWords,
        int DisassembledCandidateWords,
        int RetTerminatedCandidateWords,
        int PrologueLikeCandidateWords,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectWindowJoinEvidence(
        bool Valid,
        int TargetWindows,
        int TargetsWithIndirectTransfers,
        int TotalIndirectTransfers,
        int IndirectCalls,
        int IndirectJumps,
        int TargetsWithReferences,
        int ReferenceRows,
        int TargetsWithTransferReferenceInstructionOverlap,
        int ExactTransferReferenceInstructionRows,
        int DistinctRawWords,
        int ExecutableRawWords,
        int DisassembledCandidateWords,
        int RetTerminatedCandidateWords,
        int PrologueLikeCandidateWords,
        string Status,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectIndexedTableEvidence(
        bool Valid,
        int IndexedRows,
        int DistinctTableBases,
        int TablesScanCapped,
        int TablesTerminatedAfterNonExecutableWords,
        int ReadableTableWords,
        int ExecutableTableWords,
        int NonExecutableTableWords,
        int UnreadableTableWords,
        int DistinctExecutableWords,
        int TablesWithContiguousExecutablePrefix,
        int MaxContiguousExecutablePrefix,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderCommandDispatchEvidence(
        bool Valid,
        string XbeSha256,
        string EaglReportSha256,
        int EaglRows,
        int CompleteCommandStreamRows,
        int CommandOccurrences,
        int UniqueObservedCommandIds,
        int MappedObservedCommandIds,
        int ExecutableDispatchTablePrefixEntries,
        int DistinctDispatchTargets,
        string DispatchTableBase,
        int FirstNonExecutableIndex,
        string FirstNonExecutableWord,
        string Status,
        string Provenance,
        string DispatchBinding,
        string SemanticBinding,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectIndexedTargetEvidence(
        bool Valid,
        int IndexedTableRows,
        int CandidateEntryOccurrences,
        int DistinctCandidateTargets,
        int MappedExecutableTargets,
        int DisassembledCandidateTargets,
        int RetTerminatedCandidateTargets,
        int PrologueLikeCandidateTargets,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectIndexedAccessorEvidence(
        bool Valid,
        int IndexedTableRows,
        int CandidateEntryOccurrences,
        int DistinctCandidateTargets,
        int MappedExecutableTargets,
        int DisassembledCandidateTargets,
        int RetTerminatedCandidateTargets,
        int FallbackPopPopRetShapes,
        int SingleMovMemoryRetShapes,
        int LeaPushCallRetShapes,
        int OtherShapes,
        int DistinctMemoryDisplacements,
        int DistinctDirectCallTargets,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct CandidateFetchAssociationEvidence(
        bool Valid,
        int CandidateCount,
        int CandidateAssetsLoaded,
        int CandidateAssetsWithCompleteFiveSlotLayouts,
        int PrimitiveRows,
        int CompleteFiveSlotPrimitiveRows,
        int SlotRows,
        int ValidSlotRows,
        int RejectedCandidates,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalCandidateSlotProfileEvidence(
        bool Valid,
        int OriginalStreamRows,
        int CandidatePrimitiveRows,
        int CandidateCompleteFiveSlotRows,
        int CandidateSlot1Rows,
        int SymbolMatchRows,
        int ObjectMatchRows,
        int Category11Rows,
        int FiniteFloat2Rows,
        int NearUnitRangeFloat2Rows,
        int StreamStrideMatchRows,
        int ByteLengthMatchRows,
        int ElementCountMatchRows,
        int CompleteControlRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalCandidateRawStreamIdentityEvidence(
        bool Valid,
        int CandidateObjectCount,
        int CandidatePrimitiveRows,
        int CandidateCompleteFiveSlotRows,
        int CandidateSlotRows,
        int RawObjectsLoaded,
        int RawStreamsLoaded,
        int RawValidSlotRows,
        int RawMetadataExactRows,
        int RawPayloadHashRows,
        int CompleteIdentityRows,
        int ExpectedUnmaterializedRows,
        int RejectedObjects,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalCandidateSlotCategoryControlEvidence(
        bool Valid,
        int CandidateRows,
        int RawIdentityRows,
        int CandidateObjectRows,
        int RawIdentityObjectRows,
        int JoinedRows,
        int CandidateCompleteRows,
        int RawIdentityCompleteRows,
        int ExpectedUnmaterializedRows,
        int CandidateSlotRows,
        int SymbolFormatRows,
        int CategoryExactRows,
        int OrdinalExactRows,
        int CompleteControlRows,
        int MissingCandidateKeys,
        int MissingRawIdentityKeys,
        string Status,
        string OriginalSemantics);

    private readonly record struct OriginalRenderChainEvidence(
        bool Valid,
        int ModelPackageCandidates,
        int ModelPackagesLoaded,
        int ObjectCandidates,
        int ObjectsLoaded,
        int ModelSymbolRows,
        int BoundsSymbolRows,
        int RenderMethodSymbolRows,
        int VertexBufferSymbolRows,
        int ValidatedVertexStreamRows,
        int ObjectsWithBounds,
        int ObjectsWithValidatedVertexStreams,
        int PrimitiveRows,
        int ValidChainRows,
        int CompleteCommandStreams,
        int StateRelocationRows,
        int ShapeNameStateRows,
        int PrimitiveTypeStateRows,
        int FetchSlotRows,
        int CompleteFiveSlotFetchRows,
        int StateFetchOverlapRows,
        int StateShapePrimitiveTypeOverlapRows,
        int RejectedPackages,
        int RejectedObjects,
        string Status,
        string Provenance,
        string OriginalSemantics,
        string StateFetchControlStatus,
        string VertexRecordControlStatus);

    private readonly record struct OriginalVertexRecordAssociationEvidence(
        bool Valid,
        int ModelPackageCandidates,
        int ModelPackagesLoaded,
        int ObjectCandidates,
        int ObjectsLoaded,
        int ValidatedVertexStreamRows,
        int ObjectsWithValidatedVertexStreams,
        int PrimitiveRows,
        int RowsWithOpaqueOrdinalRecords,
        int RowsWithUniqueOpaqueSequences,
        int CompleteFiveSlotFetchRows,
        int RejectedPackages,
        int RejectedObjects,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalVertexRecordAssociationControlEvidence(
        bool Valid,
        int InputRows,
        int ObjectRows,
        int InputUniqueRows,
        int RecomputedExactRows,
        int ReverseStrideMatchRows,
        int ShiftedOrdinalMatchRows,
        int CompleteFiveSlotRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalPrimitiveStreamOpaqueDiscriminationEvidence(
        bool Valid,
        int PrimitiveRows,
        int CompleteFetchRows,
        int CandidateOrdinalRows,
        int StrideMatchingCandidateRows,
        int MetadataMatchingCandidateRows,
        int PayloadPrefixMatchingCandidateRows,
        int CurrentOrdinalRows,
        int CurrentOrdinalMetadataMatchingRows,
        int CurrentOrdinalPayloadPrefixMatchingRows,
        int UniquelyDiscriminatedByMetadataRows,
        int UniquelyDiscriminatedByPayloadPrefixRows,
        int ShiftedStrideCandidateRows,
        int ShiftedMetadataMatchingRows,
        int ShiftedPayloadPrefixMatchingRows,
        int RejectedPackages,
        int RejectedObjects,
        string Status,
        string Provenance,
        string OriginalSemantics,
        string ControlStatus,
        string ControlProvenance);

    private readonly record struct OriginalPrimitiveStreamCommandPayloadIdentityEvidence(
        bool Valid,
        int PrimitiveRows,
        int CompleteFetchRows,
        int CandidateOrdinalRows,
        int RawIdentityCandidateRows,
        int DistinctRawIdentityRows,
        int DuplicateRawIdentityRows,
        int CommandPayloadExactCandidateRows,
        int CurrentOrdinalRows,
        int CurrentOrdinalRawIdentityUniqueRows,
        int CurrentOrdinalCommandPayloadExactRows,
        int CurrentOrdinalUniquelyDiscriminatedByCommandPayloadRows,
        int ShiftedOrdinalCommandPayloadExactRows,
        int RejectedPackages,
        int RejectedObjects,
        string Status,
        string Provenance,
        string OriginalSemantics,
        string ControlStatus,
        string ControlProvenance);

    private readonly record struct OriginalPrimitiveStreamCommandDescriptorAdjacencyEvidence(
        bool Valid,
        uint CommandDescriptorDelta,
        int PrimitiveRows,
        int CompleteCommandRows,
        int CandidateOrdinalRows,
        int IdentityCandidateRows,
        int CurrentOrdinalRows,
        int CurrentOrdinalIdentityRows,
        int UniqueCurrentOrdinalIdentityRows,
        int ShiftedOrdinalIdentityRows,
        int SameSectionIdentityRows,
        int ExactAdjacencyStreamRows,
        string ExactAdjacencyCategoryHistogram,
        string ExactAdjacencyStrideHistogram,
        int RejectedPackages,
        int RejectedObjects,
        string Status,
        string Provenance,
        string OriginalSemantics,
        string ControlStatus,
        string ControlProvenance);

    private readonly record struct OriginalStateFetchAdjacencyControlEvidence(
        bool Valid,
        int RenderRows,
        int AdjacencyRows,
        int JoinedRows,
        int StateRelocationRows,
        int ShapeNameRows,
        int PrimitiveTypeRows,
        int CompleteFiveSlotFetchRows,
        int ReportedStateFetchOverlapRows,
        int RecomputedStateFetchOverlapRows,
        int JoinedExactAdjacencyRows,
        int JoinedStateExactAdjacencyRows,
        int JoinedStateFetchExactAdjacencyRows,
        string ExactAdjacencyCategoryHistogram,
        string StateExactAdjacencyCategoryHistogram,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalPrimitiveFetchTopologyControlEvidence(
        bool Valid,
        int RenderRows,
        int TopologyRows,
        int JoinedRows,
        int RenderInputStatusValidRows,
        int TopologyInputStatusValidRows,
        int TopologyGroupsWithFiveRows,
        int TopologyValidSlotRows,
        int TopologyKeyOrdinalMatchRows,
        int TopologyValidPayloadRangeRows,
        int TopologyStructuralMatchRows,
        int RenderFetchSlotRows,
        int RenderCompleteFiveSlotRows,
        int TopologyCompleteFiveSlotRows,
        int FetchSlotCountMatchRows,
        int FetchSlotIdSetMatchRows,
        int CompleteFiveSlotSequenceAgreementRows,
        int IncompleteSlotTwoRows,
        int MissingRenderKeys,
        int MissingTopologyKeys,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalCommandPayloadRegionControlEvidence(
        bool Valid,
        int RenderRows,
        int TopologyRows,
        int JoinedRows,
        int RenderInputStatusValidRows,
        int TopologyInputStatusValidRows,
        int TopologyGroupsWithFiveRows,
        int PayloadRangeValidRows,
        int PayloadIntervalNonoverlapRows,
        int CommandSectionMatchRows,
        int CommandIntervalValidRows,
        int CommandPayloadNonoverlapRows,
        int CommandAfterPayloadEndRows,
        int CommandPayloadGapEquals0x10Rows,
        string CommandPayloadGapHistogram,
        int IncompleteSlotTwoRows,
        int MissingRenderKeys,
        int MissingTopologyKeys,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalVertexPayloadEvidence(
        bool Valid,
        int ModelPackageCandidates,
        int ModelPackagesLoaded,
        int ObjectCandidates,
        int ObjectsLoaded,
        int ValidatedVertexStreamRows,
        int ObjectsWithValidatedVertexStreams,
        int ObjectsWithFinitePackedFloatProfiles,
        int ObjectsWithFiniteFloat2Profiles,
        int ObjectsWithDeclaredBoundsContainment,
        int PayloadRows,
        int ObjectsWithDistinctPayloadRanges,
        int ObjectsWithOverlappingPayloadRanges,
        int RejectedPackages,
        int RejectedObjects,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalVertexPayloadControlEvidence(
        bool Valid,
        int InputRows,
        int ObjectRows,
        int NumericSignatureMatchRows,
        int PayloadRangeMetadataValidRows,
        int InputStatusValidRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalNumericStreamAmbiguityEvidence(
        bool Valid,
        int InputRows,
        int ValidInputRows,
        int ObjectsWithNoBoundsMatch,
        int ObjectsWithUniqueBoundsMatch,
        int ObjectsWithMultipleBoundsMatches,
        int ObjectsWithNonUniqueOrMissingMatch,
        string BoundsMatchCardinalityHistogram,
        string SemanticBinding,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalBoundsConsistentRecordEvidence(
        bool Valid,
        int InputRows,
        int ObjectOrdinalGroups,
        int CompleteFiveStreamGroups,
        int CategorySequenceValidGroups,
        int ValidStreamRows,
        int FirstRawStride12Groups,
        int FirstRawBoundsMatchGroups,
        int FirstRawNoBoundsMatchGroups,
        int LaterRawBoundsMatchGroups,
        string AllBoundsMatchesAreFirstRaw,
        string FirstRawCategoryHistogram,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalBoundsFetchSlotJoinEvidence(
        bool Valid,
        int NumericRows,
        int TopologyRows,
        int JoinedRows,
        int MissingNumericKeys,
        int MissingTopologyKeys,
        int ObjectOrdinalGroups,
        int CompleteFiveStreamGroups,
        int IdentityJoinRows,
        int NumericInputStatusValidRows,
        int TopologyInputStatusValidRows,
        int FirstRawFetchSlotZeroGroups,
        int FirstRawStrideMatchGroups,
        int FirstRawBoundsMatchGroups,
        int FirstRawNoBoundsMatchGroups,
        int LaterRawBoundsMatchGroups,
        int IncompleteFetchSlotGroups,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalNumericSlotRoleDiscriminationEvidence(
        bool Valid,
        int NumericRows,
        int TopologyRows,
        int JoinedRows,
        int PerPrimitiveSlotRows,
        int MissingNumericKeys,
        int MissingTopologyKeys,
        int DuplicateNumericKeys,
        int DuplicateTopologyKeys,
        int PrimitiveGroups,
        int GroupsWithFiveRows,
        int CompleteFiveSlotGroups,
        int CompleteGroupsWithUniqueNearUnitFloat2,
        int CompleteGroupsWithNearUnitFloat2,
        int CompleteGroupsWithSlot1NearUnitFloat2,
        int CompleteGroupsWithOtherSlotNearUnitFloat2,
        int CompleteGroupsWithSlot0BoundsMatch,
        int CompleteGroupsWithNoSlot0BoundsMatch,
        int IncompleteFiveSlotGroups,
        int NumericInputStatusValidRows,
        int TopologyInputStatusValidRows,
        string SlotRowCountHistogram,
        string NearUnitFloat2SlotHistogram,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateNumericSlotCooccurrenceEvidence(
        bool Valid,
        int StateRows,
        int RoleRows,
        int JoinedRows,
        int MissingStateKeys,
        int MissingRoleKeys,
        int DuplicateStateKeys,
        int DuplicateRoleKeys,
        int StateInputStatusValidRows,
        int RoleInputStatusValidRows,
        int PrimitiveGroups,
        int GroupsWithFiveRoleRows,
        int CompleteFiveSlotGroups,
        int StateRelocationRows,
        int ShapenameStateRows,
        int PrimitiveTypeStateRows,
        int ExactAdjacencyStreamRows,
        int CompleteGroupsWithShapenameState,
        int CompleteGroupsWithoutShapenameState,
        int CompleteGroupsWithPrimitiveTypeState,
        int CompleteGroupsWithoutPrimitiveTypeState,
        int CompleteGroupsWithUniqueNearUnitFloat2,
        int CompleteGroupsWithSlot1NearUnitFloat2,
        int CompleteGroupsWithOtherSlotNearUnitFloat2,
        int IncompleteFiveSlotGroups,
        string RoleRowCountHistogram,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalCommandSlotRoleCooccurrenceEvidence(
        bool Valid,
        int DescriptorRows,
        int RoleRows,
        int JoinedRows,
        int MissingDescriptorKeys,
        int MissingRoleKeys,
        int DuplicateDescriptorKeys,
        int DuplicateRoleKeys,
        int DescriptorInputStatusValidRows,
        int RoleInputStatusValidRows,
        int CurrentOrdinalPresentRows,
        int CurrentOrdinalIdentityMatchRows,
        int CurrentOrdinalUniquelyIdentifiedRows,
        int ShiftedOrdinalIdentityMatchRows,
        int PrimitiveGroups,
        int GroupsWithFiveRoleRows,
        int CompleteFiveSlotGroups,
        int CompleteGroupsWithUniqueNearUnitFloat2,
        int CompleteGroupsWithSlot1NearUnitFloat2,
        int CompleteGroupsWithOtherSlotNearUnitFloat2,
        int IncompleteFiveSlotGroups,
        string RoleRowCountHistogram,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalRenderStateGeometryBoundsControlEvidence(
        bool Valid,
        int PartitionObjectRows,
        int BoundsObjectRows,
        int JoinedObjectRows,
        int PartitionInputStatusValidRows,
        int BoundsInputStatusValidRows,
        int ObjectCardinalityMatchRows,
        int BoundsGroupRows,
        int CompleteFetchGroupRows,
        int ExpectedIncompleteFetchGroupRows,
        int BoundsSlot0MatchRows,
        int BoundsNonzeroMatchRows,
        int ExactCommandShapeRows,
        int CompleteControlRows,
        string SemanticBinding,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStatePopulationControlEvidence(
        bool Valid,
        int ObjectRows,
        int PrimitiveRows,
        int ShapeNameRows,
        int PrimitiveTypeRows,
        int PopulationOverlapRows,
        int SamePrimitiveIndexPairs,
        int AdjacentPrimitivePairs,
        int SameLayerPairs,
        int ExactCommandHashPairs,
        int ExactBoundaryShapePairs,
        int CompleteShapeNameRows,
        int CompletePrimitiveTypeRows,
        int ObjectsWithRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateTokenEvidence(
        bool Valid,
        int InputRows,
        int StateRows,
        int ValidInputRows,
        int ShapeNameRows,
        int ClampURows,
        int ClampVRows,
        int PrimitiveTypeRows,
        int TextureTokenRows,
        int MaterialTokenRows,
        int UvCoordinateTokenRows,
        int VertexAttributeTokenRows,
        int DeformationTokenRows,
        string AssignmentNameCatalog,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateCandidateAttributeControlEvidence(
        bool Valid,
        int CandidateObjectRows,
        int StateObjectRows,
        int InfluenceObjectRows,
        int JoinedObjectRows,
        int MissingStateObjects,
        int MissingAttributeObjects,
        int MissingInfluenceObjects,
        int StateRowsJoined,
        int StateValidRows,
        int StateNegativeTokenRows,
        int AttributeCompleteRows,
        int AttributeFiniteRows,
        int AttributeNearUnitRows,
        int InfluenceGeometryRows,
        int InfluenceStatusRows,
        int InfluenceTextureRows,
        int InfluenceMeshShapeStateExactRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct CandidateAttributeTextureCompatibilityEvidence(
        bool Valid,
        int AttributeObjectRows,
        int AssetObjectRows,
        int JoinedObjectRows,
        int AttributeInputStatusValidRows,
        int AssetInputStatusValidRows,
        int TextureDimensionsValidRows,
        int TexturePal8Rows,
        int DecodedPixelMatchRows,
        int Slot1RangeRows,
        int Slot1FiniteRangeRows,
        int Slot1NormalizedRangeRows,
        int Slot1ValueCountMatchVertexRows,
        int AttributeTextureCompatibilityRows,
        string SemanticBinding,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalShapenameCandidateAssetControlEvidence(
        bool Valid,
        int StateRows,
        int CandidateRows,
        int JoinedRows,
        int MissingCandidateObjects,
        int MissingStateObjects,
        int StateInputValidRows,
        int CandidateInputValidRows,
        int ObjectStateExactRows,
        int ShapenameTokenMatchRows,
        int SameTrunkRows,
        int SourceChainValidRows,
        int ShapeEntryExactRows,
        int DecodedPixelMatchRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateCandidatePrimitiveShapeControlEvidence(
        bool Valid,
        int StateRows,
        int CandidateRows,
        int JoinedRows,
        int MissingCandidateObjects,
        int MissingStateObjects,
        int StateInputValidRows,
        int CandidateInputValidRows,
        int StatePrimitiveCountMatchRows,
        int ShapenameCountMatchRows,
        int CandidateShapeStateStreamsCompleteRows,
        int SameTrunkRows,
        int SourceChainValidRows,
        int ShapeEntryExactRows,
        int DecodedPixelMatchRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalCandidateShapenamePrimitiveIndexControlEvidence(
        bool Valid,
        int StateRows,
        int CandidateRows,
        int JoinedRows,
        int MissingCandidateObjects,
        int MissingStateObjects,
        int SequenceMatchRows,
        int SetMatchRows,
        int CountMatchRows,
        int CompleteCandidateRows,
        int OriginalInputValidRows,
        int CandidateInputValidRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalCandidatePrimitiveFetchControlEvidence(
        bool Valid,
        int OriginalRows,
        int CandidateRows,
        int JoinedRows,
        int MissingCandidateKeys,
        int MissingOriginalKeys,
        int CommandCountMatchRows,
        int CommandBytesSha256MatchRows,
        int CommandStreamCompleteMatchRows,
        int FetchSlotCountMatchRows,
        int FetchSlotIdSequenceMatchRows,
        int CompleteFiveSlotSequenceMatchRows,
        int StateAssignmentSequenceMatchRows,
        int PrimitiveTypeFlagMatchRows,
        int CandidateSlotRecordsValidRows,
        int CandidateSlotRecordsUnmaterializedExpectedRows,
        int OriginalInputValidRows,
        int CandidateInputValidRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalGeometryDrawClassificationEvidence(
        bool Valid,
        int CandidateObjectRows,
        int OriginalScopedRows,
        int CandidateRows,
        int JoinedRows,
        int MissingOriginalKeys,
        int MissingCandidateKeys,
        int JoinedObjectRows,
        int ObjectsWithCompleteClassification,
        int OriginalInputStatusValidRows,
        int CandidateInputStatusValidRows,
        int OriginalGeometryMarkerRows,
        int OriginalStateMarkerRows,
        int CandidateGeometryMarkerRows,
        int CandidateStateMarkerRows,
        int ClassificationMatchRows,
        int StateCountGreaterThanGeometryRows,
        int StateToGeometryUniqueBindingProvenRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateCandidateMaterialAmbiguityEvidence(
        bool Valid,
        int CandidateObjectRows,
        int RenderScopedRows,
        int OrderRows,
        int OrderStateRows,
        int OriginalGeometryRows,
        int OriginalStateRows,
        int NonemptyStateIntervalRows,
        int IntervalsWithTwoAdjacentGeometryCandidates,
        int IntervalsWithOneAdjacentGeometryCandidate,
        int IntervalsWithZeroAdjacentGeometryCandidates,
        int StateRowsJoined,
        int StateRowsWithShapeTokenMatch,
        int StateRowsWithCandidateAssetSource,
        int CandidateAssetValidRows,
        int ObjectAssetJoinRows,
        int ObjectsWithCompleteAmbiguityControl,
        int OriginalInputStatusValidRows,
        int OrderInputStatusValidRows,
        int StateAssetInputStatusValidRows,
        int CandidateAssetInputStatusValidRows,
        int StateToGeometryUniqueBindingProvenRows,
        int StateToMaterialUniqueBindingProvenRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalCandidateStreamRecordControlEvidence(
        bool Valid,
        int OriginalRows,
        int OriginalPrimitiveRows,
        int CandidateRows,
        int JoinedPrimitiveRows,
        int MissingCandidateKeys,
        int MissingOriginalKeys,
        int CandidateCompletePrimitiveRows,
        int CandidateIncompletePrimitiveRows,
        int CandidateSlotRowsMaterialized,
        int JoinedMaterializedSlotRows,
        int SymbolExactRows,
        int AuxiliaryExactRows,
        int FetchStrideExactRows,
        int StreamStrideExactRows,
        int ByteLengthExactRows,
        int ElementCountExactRows,
        int RecordExactRows,
        int CandidateSlotRecordsUnmaterializedExpectedRows,
        int OriginalInputValidRows,
        int CandidateInputValidRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalCandidateGeometryStreamControlEvidence(
        bool Valid,
        int OriginalSlotZeroRows,
        int CandidateRows,
        int JoinedRows,
        int MissingCandidateKeys,
        int MissingOriginalKeys,
        int CandidateSourceSlotZeroRows,
        int CandidateCompleteRows,
        int CandidateFiniteAndIndexedRows,
        int SymbolExactRows,
        int StreamStrideExactRows,
        int ByteLengthExactRows,
        int ElementCountExactRows,
        int VertexCountExactRows,
        int TriangleCountExactRows,
        int OriginalInputValidRows,
        int CandidateInputValidRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalCandidateGeometryPayloadIdentityEvidence(
        bool Valid,
        int GeometryRows,
        int CandidateGeometryRows,
        int JoinedRows,
        int MissingCandidateGeometryKeys,
        int MissingCandidateFetchKeys,
        int MissingRawIdentityKeys,
        int OriginalSlotRows,
        int CandidateSlotRows,
        int RawHashRows,
        int SymbolExactRows,
        int MetadataExactRows,
        int PayloadHashExactRows,
        int CompleteIdentityRows,
        int OriginalInputValidRows,
        int CandidateInputValidRows,
        int GeometryInputValidRows,
        int RawInputValidRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalRenderPayloadObjectJoinEvidence(
        bool Valid,
        int RenderObjectRows,
        int PayloadObjectRows,
        int JoinedObjectRows,
        int RenderOnlyObjects,
        int PayloadOnlyObjects,
        int RenderPrimitiveRows,
        int PayloadValidatedStreamRows,
        int JoinedIdentityMatchRows,
        int RenderStatusValidRows,
        int PayloadStatusValidRows,
        int StructuralCountsValidRows,
        int ReversedKeyMatchRows,
        int ShiftedKeyMatchRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateGeometryPartitionControlEvidence(
        bool Valid,
        int AdjacencyRows,
        int ProfileRows,
        int OrderRows,
        int JoinedObjectRows,
        int AdjacencyInputStatusValidRows,
        int ProfileInputStatusValidRows,
        int OrderInputStatusValidRows,
        int ObjectStateExactRows,
        int GeometryGroupStateExactRows,
        int ProfileGeometryPairRows,
        int ProfileStatePairRows,
        int ProfileRowsReconciled,
        int OrderPartitionedStates,
        int AdjacencyStates,
        int OrderStates,
        int StateIndexSetMatchRows,
        int IntervalIndexSequenceMatchRows,
        int IntervalBoundaryShapeMatchRows,
        int CompleteProfileStreamRows,
        int ExactCommandShapeRows,
        bool ExactCommandShapeExpectedZero,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateGeometryBindingAmbiguityControlEvidence(
        bool Valid,
        int AdjacencyRows,
        int ProfileRows,
        int OrderRows,
        int PartitionRows,
        int JoinedObjectRows,
        int MissingProfileObjects,
        int MissingOrderObjects,
        int MissingPartitionObjects,
        int AdjacencyInputStatusValidRows,
        int ProfileInputStatusValidRows,
        int OrderInputStatusValidRows,
        int PartitionInputStatusValidRows,
        int ProfileCardinalityReconciledRows,
        int OrderCardinalityReconciledRows,
        int OrderStateIndexSetReconciledRows,
        int StateCountGreaterThanGeometryRows,
        int GeometryGroupStateExactRows,
        int ExactCommandShapeRows,
        int UniqueGeometryStateBindingProvenRows,
        int ValidObjectRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateGeometryGlobalAssignmentControlEvidence(
        bool Valid,
        int OrderRows,
        int PartitionRows,
        int JoinedObjectRows,
        int MissingPartitionObjects,
        int OrderInputStatusValidRows,
        int PartitionInputStatusValidRows,
        int NonemptyIntervalRows,
        int AmbiguousIntervalRows,
        int ObjectsWithMultipleAssignmentModels,
        int ObjectsWithUniqueAssignmentModel,
        int ObjectsWithOneToOneUniqueModel,
        int UnconstrainedAssignmentModelSum,
        int OneToOneAssignmentModelSum,
        int StateIndexSetReconciledRows,
        int UniqueStateGeometryAssignmentProvenRows,
        int ValidObjectRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateGeometryUvMaterialGlobalAssignmentControlEvidence(
        bool Valid,
        int GlobalRows,
        int UvRows,
        int MaterialRows,
        int JoinedObjectRows,
        int MissingUvObjects,
        int MissingMaterialObjects,
        int GlobalInputStatusValidRows,
        int UvCandidateAttributeValidRows,
        int UvCandidateRasterInfluenceValidRows,
        int UvCompleteGroupRows,
        int UvIncompleteGroupRows,
        int UvSlot1NearUnitGroupRows,
        int UvOtherSlotNearUnitGroupRows,
        int MaterialInputStatusValidRows,
        int MaterialCandidateAssetValidRows,
        int MaterialStateCandidateSourceCompleteRows,
        int ObjectsWithMultipleGeometryAssignmentModels,
        int ObjectsWithSingleGeometryAssignmentModel,
        int ObjectsWithSingleCandidateUvRoleDomain,
        int ObjectsWithSingleCandidateMaterialDomain,
        int StructuralCrossProductModelSum,
        int ObjectsWithMultipleStructuralCrossProductModels,
        int ObjectsWithSingleStructuralCrossProductModel,
        int UniqueStateGeometryUvMaterialBindingProvenRows,
        int ValidObjectRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateGeometryCandidateRenderGlobalAssignmentControlEvidence(
        bool Valid,
        int GlobalRows,
        int RuntimeRows,
        int CandidateRows,
        int PayloadJoinRows,
        int JoinedObjectRows,
        int MissingRuntimeObjects,
        int MissingCandidateObjects,
        int MissingPayloadJoinObjects,
        int GlobalInputStatusValidRows,
        int RuntimeInputStatusValidRows,
        int CandidateInputStatusValidRows,
        int PayloadJoinInputStatusValidRows,
        int GeometryCandidateCountSum,
        int CandidateGeometryGroupSum,
        int GeometryCandidateCardinalityMatchRows,
        int ObjectStateExactRows,
        int RenderGroupStateMatchRows,
        int CandidateSourceChainValidRows,
        int PayloadObjectJoinValidRows,
        int ObjectsWithMultipleGeometryAssignmentModels,
        int ObjectsWithSingleGeometryAssignmentModels,
        int UnconstrainedAssignmentModelSum,
        int OneToOneAssignmentModelSum,
        int UniqueStateGeometryCandidateRenderBindingProvenRows,
        int ValidObjectRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence(
        bool Valid,
        int GlobalRows,
        int PayloadIdentityRows,
        int JoinedObjectRows,
        int MissingPayloadIdentityObjects,
        int GlobalInputStatusValidRows,
        int PayloadIdentityInputStatusValidRows,
        int GeometryCandidateCountSum,
        int PayloadIdentityGeometryRowSum,
        int PayloadIdentityCompleteGeometrySum,
        int PayloadIdentityRawHashRowSum,
        int PayloadIdentityCardinalityMatchRows,
        int PayloadIdentityReducedStateDomainRows,
        int ObjectsWithMultipleAssignmentModels,
        int ObjectsWithSingleAssignmentModels,
        int AssignmentModelSumBeforePayloadIdentity,
        int AssignmentModelSumAfterPayloadIdentity,
        int OneToOneModelSumBeforePayloadIdentity,
        int OneToOneModelSumAfterPayloadIdentity,
        int UniqueStateGeometryPayloadBindingProvenRows,
        int ValidObjectRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateGeometryDrawClassificationGlobalAssignmentControlEvidence(
        bool Valid,
        int GlobalRows,
        int DrawClassificationRows,
        int PayloadIdentityRows,
        int JoinedObjectRows,
        int MissingDrawClassificationObjects,
        int MissingPayloadIdentityObjects,
        int GlobalInputStatusValidRows,
        int DrawClassificationInputStatusValidRows,
        int PayloadIdentityInputStatusValidRows,
        int GeometryCandidateCountSum,
        int DrawClassificationGeometryMarkerSum,
        int PayloadIdentityGeometryRowSum,
        int ClassificationMatchRows,
        int GeometryDrawPayloadIdentityJoinRows,
        int StateMarkerRows,
        int StateRowsWithoutPayloadIdentity,
        int ObjectsWithStateOverGeometryRows,
        int AssignmentModelSumBeforeDrawClassification,
        int AssignmentModelSumAfterDrawClassification,
        int OneToOneModelSumBeforeDrawClassification,
        int OneToOneModelSumAfterDrawClassification,
        int DrawClassificationReducedStateDomainRows,
        int UniqueStateGeometryDrawBindingProvenRows,
        int ValidObjectRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateUidAmbiguityControlEvidence(
        bool Valid,
        int CandidateObjectRows,
        int RenderScopedRows,
        int JoinedObjectRows,
        int MissingCandidateObjects,
        int MissingRenderObjects,
        int GeometryRows,
        int StateRows,
        int GeometryRowsWithValidUid,
        int StateRowsWithValidUid,
        int SharedUidCount,
        int GeometryRowsSameUidStateCandidates,
        int GeometryRowsZeroSameUidStateCandidates,
        int GeometryRowsOneSameUidStateCandidate,
        int GeometryRowsMultipleSameUidStateCandidates,
        int StateRowsInSharedUidDomains,
        int ObjectsWithUidAmbiguity,
        int UniqueGeometryStateBindingProvenRows,
        int CandidateInputStatusValidRows,
        int RenderInputStatusValidRows,
        int ValidObjectRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateUidCommandHashAmbiguityControlEvidence(
        bool Valid,
        int CandidateObjectRows,
        int RenderScopedRows,
        int JoinedObjectRows,
        int MissingCandidateObjects,
        int MissingRenderObjects,
        int GeometryRows,
        int StateRows,
        int GeometryRowsWithValidUid,
        int StateRowsWithValidUid,
        int SharedUidCount,
        int GeometryRowsSameUidStateCandidates,
        int GeometryRowsZeroSameUidStateCandidates,
        int GeometryRowsOneSameUidStateCandidate,
        int GeometryRowsMultipleSameUidStateCandidates,
        int GeometryRowsWithUidAndCommandHashCandidates,
        int GeometryRowsWithZeroUidAndCommandHashCandidates,
        int GeometryRowsWithOneUidAndCommandHashCandidate,
        int GeometryRowsWithMultipleUidAndCommandHashCandidates,
        int StateRowsInSharedUidDomains,
        int ObjectsWithUidAmbiguity,
        int ObjectsWithUidAndCommandHashAmbiguity,
        int UniqueGeometryStateBindingProvenRows,
        int UniqueGeometryStateBindingWithCommandHashProvenRows,
        int CandidateInputStatusValidRows,
        int RenderInputStatusValidRows,
        int ValidObjectRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateProfileDiscriminationControlEvidence(
        bool Valid,
        int ProfileRows,
        int ProfileObjects,
        int GeometryCandidateRows,
        int StatePrimitiveRows,
        int DistinctObjectStateProfileSignatures,
        int ProfileTieCountMatchRows,
        int StatePrimitivesWithMultipleGeometryCandidates,
        int StatePrimitivesWithSingleGeometryCandidate,
        int StatePrimitivesWithZeroGeometryCandidates,
        int StateSignatureGroupsWithMultipleGeometryCandidates,
        int ExactCommandShapeRows,
        int UniqueStateToGeometryBindingProvenRows,
        int ValidObjectRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateSlot1CandidateUvAmbiguityEvidence(
        bool Valid,
        int StateRows,
        int RoleRows,
        int JoinedPrimitiveGroups,
        int CandidateJoinedPrimitiveGroups,
        int MissingStateKeys,
        int MissingRoleKeys,
        int DuplicateStateKeys,
        int DuplicateRoleKeys,
        int CompleteFiveSlotGroups,
        int IncompleteFiveSlotGroups,
        int ShapenameStateGroups,
        int CompleteShapenameStateGroups,
        int CompleteShapenameSlot1NearUnitGroups,
        int CompleteShapenameOtherSlotNearUnitGroups,
        int CompleteGroupsSlot1NearUnitGroups,
        int CompleteGroupsOtherSlotNearUnitGroups,
        int CandidateObjectRows,
        int CandidateAttributeTextureValidRows,
        int CandidateRasterInfluenceValidRows,
        int UniqueStateToGeometryBindingProvenRows,
        int ValidObjectRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct AnimationMorphPoseBankAmbiguityControlEvidence(
        bool Valid,
        int PoseRows,
        int DistinctPoseArchives,
        int DistinctPoseSymbols,
        int DistinctNormalizedNames,
        int AnimationBankRows,
        int DistinctAnimationBanks,
        int AnimationBankValidRows,
        int AnimationBankSymbolMetadataRows,
        int MorphLayoutRows,
        int MorphParseValidRows,
        int MorphContainingPairs,
        int MorphExactGapSetLayouts,
        int MorphExactGapSetPairs,
        int MorphExactGapHistogramLayouts,
        int MorphExactGapHistogramPairs,
        int PoseNameMatchRows,
        int PoseSymbolMetadataMatchRows,
        int UniquePoseAnimationBindingProvenRows,
        int ValidPoseRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct AnimationCatalogPoseTimingGlobalAssignmentControlEvidence(
        bool Valid,
        int CatalogBankCount,
        int CatalogRows,
        int CatalogTimestampValidRows,
        int AnchorBankRows,
        int AnchorCompleteBankRows,
        int AnchorCount,
        int PoseRows,
        int DistinctPoseArchives,
        int DistinctPoseSymbols,
        int DistinctNormalizedNames,
        int MorphLayoutRows,
        int MorphParseValidRows,
        int MorphContainingLayouts,
        int MorphContainingPairs,
        int MorphUniqueContainmentLayouts,
        int MorphAmbiguousContainmentLayouts,
        int MorphExactGapSetPairs,
        int PoseRowsWithoutCatalogJoinKey,
        int PoseRowsWithoutMorphJoinKey,
        int PoseBankDomainSizeBefore,
        int PoseBankDomainSizeAfter,
        int GlobalAssignmentModelsBefore,
        int GlobalAssignmentModelsAfter,
        int PoseBankDomainReducedRows,
        int UniquePoseAnimationBindingProvenRows,
        int ValidPoseRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalStateGeometryPayloadJoinControlEvidence(
        bool Valid,
        int AdjacencyRows,
        int JoinRows,
        int JoinedObjectRows,
        int MissingJoinObjects,
        int AdjacencyInputStatusValidRows,
        int JoinInputStatusValidRows,
        int IdentityJoinMatchRows,
        int RenderStatusValidRows,
        int PayloadStatusValidRows,
        int StructuralCountsValidRows,
        int PrimitiveCountMatchRows,
        int CompleteCommandCountMatchRows,
        int FetchCountMatchRows,
        int FiveSlotPayloadCountMatchRows,
        int DistinctPayloadRangeCountMatchRows,
        int PayloadOverlapZeroRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct AudioPayloadPolicyControlEvidence(
        bool Valid,
        int BoundedRows,
        int AllRows,
        int JoinedRows,
        int BoundedEventRows,
        int AllEventRows,
        int BoundedBankRows,
        int AllBankRows,
        int IdentityMatchRows,
        int StatusMatchRows,
        int DecodeMatchRows,
        int WaveBytesMatchRows,
        int PcmHashMatchRows,
        int ExactMatchCountMatchRows,
        int ExactPcmMatchRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct AudioNamedPayloadIdentityControlEvidence(
        bool Valid,
        int RuntimeRows,
        int NamedDecodeRows,
        int CorrelationEventRows,
        int CorrelationBankRows,
        int CorrelationBankDecodedRows,
        int JoinedEventRows,
        int MissingRuntimeEvents,
        int MissingNamedDecodeEvents,
        int MissingCorrelationEvents,
        int RuntimeMetadataValidatedRows,
        int RuntimePreparedRows,
        int RuntimeWaveHeaderValidRows,
        int NamedDecodeValidatedRows,
        int CorrelationDecodeValidatedRows,
        int WaveBytesMatchRows,
        int PcmBytesMatchRows,
        int PcmSha256MatchRows,
        int EventHashesWithBankMatch,
        int ExactPcmHashPairs,
        int UniqueEventNames,
        string Status,
        string Provenance,
        string DirectSelectionBinding,
        string OriginalSemantics);

    private readonly record struct AudioBankDecoderSelectionControlEvidence(
        bool Valid,
        int BatchRows,
        int ExhaustiveBankRows,
        int UniqueBatchBanks,
        int UniqueExhaustiveBanks,
        int BatchDecodedRows,
        int BatchRejectedRows,
        int SelectedIdentityRows,
        int SelectedKindMatchRows,
        int SelectedWaveBytesMatchRows,
        int RejectedPolicyMatchRows,
        int ValidControlRows,
        string Status,
        string Provenance,
        string DirectSelectionBinding,
        string OriginalSemantics);

    private readonly record struct AudioParallelRuntimeJoinControlEvidence(
        bool Valid,
        int ParallelRows,
        int RuntimeRows,
        int JoinedRows,
        int MissingRuntimeNames,
        int MissingParallelNames,
        int ParallelUniqueEventNames,
        int RuntimeUniqueEventNames,
        int HeaderRowMatchRows,
        int DataOffsetMatchRows,
        int DataLengthMatchRows,
        int CandidateMetadataValidatedRows,
        int RuntimePreparedRows,
        int WaveHeaderValidRows,
        int ParallelInputStatusRows,
        int RuntimeInputStatusRows,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct AudioEventEvtPcpRangeDiscriminationEvidence(
        bool Valid,
        int ParallelRows,
        int ValidNamedRangeRows,
        int EventStringAnchorRows,
        int HeaderRangeRows,
        int DataRangeRows,
        int ParallelRowIndexRows,
        int DataSchlRows,
        int UniqueEventNameRows,
        int UniqueEventStringOffsetRows,
        int UniqueHeaderRowRows,
        int ParallelStatusValidRows,
        int IndexCandidateNamesContainsEventRows,
        bool Index32BitAbkZero,
        bool Index32BitPcpahdrZero,
        bool Index32BitPcpadatZero,
        int RuntimeJoinedRows,
        int RuntimeMissingNames,
        string Status,
        string Provenance,
        string DirectSelectionBinding,
        string OriginalSemantics);

    private readonly record struct AudioEventAudioAssetHashUniverseEvidence(
        bool Valid,
        int CandidateCount,
        int ScannedAssetCount,
        int ValidAssetCount,
        long ScannedBytes,
        int ExcludedSourceRows,
        int AssetsWithHits,
        int AssetHashHitPairs,
        int AlignedOccurrences,
        string Status,
        string Provenance,
        string DirectSelectionBinding,
        string OriginalSemantics);

    private readonly record struct AudioEventDataAssetHashUniverseEvidence(
        bool Valid,
        string AssetRoot,
        string EventPath,
        int CandidateCount,
        int ScannedAssetCount,
        int ValidAssetCount,
        long ScannedBytes,
        int ExcludedSourceRows,
        int AssetsWithHits,
        int AssetHashHitPairs,
        int AlignedOccurrences,
        string Status,
        string Provenance,
        string DirectSelectionBinding,
        string OriginalSemantics);

    private readonly record struct AudioEventDataAssetHashUniverseControlEvidence(
        bool Valid,
        string AssetRoot,
        string EventPath,
        int CandidateCount,
        int Rows,
        int ValidRows,
        int UniquePaths,
        long ScannedBytes,
        long RecomputedBytes,
        int ExcludedSourceRows,
        int AssetsWithHits,
        int HitRows,
        int AssetHashHitPairs,
        int AlignedOccurrences,
        string HitAssetPath,
        string HitHashWord,
        string HitEventName,
        string HitOffset,
        string Status,
        string Provenance,
        string DirectSelectionBinding,
        string OriginalSemantics);

    private readonly record struct AudioEventNameTextSearchEvidence(
        bool Valid,
        int CandidateCount,
        int ScannedAssetCount,
        int ValidAssetCount,
        long ScannedBytes,
        int ExcludedSourceRows,
        int AssetNameHitPairs,
        int AssetsWithNameHits,
        int RawOccurrences,
        string Status,
        string Provenance,
        string DirectSelectionBinding,
        string OriginalSemantics);

    private readonly record struct AudioEventNameTextSearchControlEvidence(
        bool Valid,
        int InputRows,
        int DistinctAssets,
        int DistinctNames,
        int ValidRows,
        int NoHitRows,
        int RawOccurrenceRows,
        int TotalRawOccurrences,
        int DuplicateAssetNameKeys,
        bool SourceSummaryMatchesRecomputed,
        int ExpectedAssets,
        int ExpectedNames,
        string Status,
        string SemanticBinding,
        string Provenance,
        string DirectSelectionBinding,
        string OriginalSemantics);

    private readonly record struct AudioEventAbkRawIdentityEvidence(
        bool Valid,
        int EventCandidates,
        int EventRawRows,
        int EventDistinctRawHashes,
        int BankCount,
        int BankNonDummySoundEntries,
        int BankAstRangeRows,
        int BankAstSchlRows,
        int FullRawHashPairs,
        int EventHashesWithFullRawMatch,
        int RawPrefixPairs,
        int RejectedRows,
        string Status,
        string Provenance,
        string ControlStatus,
        string OriginalSemantics);

    private readonly record struct AuthoredAudioCandidateRouteControlEvidence(
        bool Valid,
        int ScenarioRows,
        int VerifiedScenarioRows,
        int TraceRows,
        int TracePlayedRows,
        int RouteRows,
        int UniqueActionRows,
        int UniqueCandidateRows,
        int RuntimeRows,
        int RuntimePreparedRouteRows,
        int RuntimeWaveHeaderValidRouteRows,
        int MissingRuntimeCandidates,
        int CoverageMissingPolicyActions,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct AuthoredNamedPayloadIdentityControlEvidence(
        bool Valid,
        int ScenarioRows,
        int VerifiedScenarioRows,
        int TraceRows,
        int TracePlayedRows,
        int IdentityRows,
        int RuntimeRows,
        int RuntimePreparedRouteRows,
        int RuntimeWaveHeaderValidRouteRows,
        int WaveBytesMatchRows,
        int PcmBytesMatchRows,
        int PcmSha256MatchRows,
        int InvalidTraceIdentityRows,
        int MissingRuntimeCandidates,
        string Status,
        string Provenance,
        string DirectSelectionBinding,
        string OriginalSemantics);

    private readonly record struct Fifast2CsiEvtNameAmbiguityControlEvidence(
        bool Valid,
        int CsiRows,
        int EvtRows,
        int CsiValidRows,
        int EvtValidRows,
        int EvtUniqueNames,
        int EvtRowsWithZeroCsiNameMatches,
        int EvtRowsWithSingleCsiNameMatch,
        int EvtRowsWithAmbiguousCsiNameMatches,
        int EvtRowsWithAnyCsiNameMatch,
        int ExactEventNameOverlapRows,
        int UniqueNameBindingProvenRows,
        string Status,
        string SemanticBinding,
        string DirectSelectionBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct AudioEventSampleGlobalAssignmentControlEvidence(
        bool Valid,
        int EventRows,
        int BankRows,
        int DecodedEventRows,
        int DecodedBankRows,
        int EventInputStatusValidRows,
        int CsiInputStatusValidRows,
        int ParallelInputStatusValidRows,
        int NegativeSynthesisValidSourceCount,
        int AuthoredRouteRows,
        int BankCandidatePoolSize,
        int EventExactPcmMatchRows,
        int EventHashesWithMatch,
        int EventRowsWithZeroExactSampleModels,
        int EventRowsWithSingleWeakNameModels,
        int EventRowsWithMultipleWeakNameModels,
        int EventRowsWithZeroWeakNameModels,
        int WeakNameDomainSum,
        int ExactIdentityModelSum,
        int StructuralCrossProductModelSum,
        int UniqueEvtAbkcSampleBindingProvenRows,
        int ValidEventRows,
        string Status,
        string SemanticBinding,
        string Provenance,
        string DirectSelectionBinding,
        string OriginalSemantics);

    private readonly record struct AudioEventStaticContextGlobalAssignmentControlEvidence(
        bool Valid,
        int StaticAudioXrefRows,
        int UniqueAudioLiterals,
        int AudioContextRows,
        int AudioContextWithExecutableCalls,
        int DistinctExecutableCallTargets,
        int AudioTargetsWithMultipleLiterals,
        int NestedAudioLiteralMatches,
        int XdvdfsAudioLiteralMatches,
        int AudioEventLiteralXrefRows,
        int EventPathCandidateNameCount,
        int EventPathCandidatePairs,
        int EventPathLiteralRows,
        int EventPathLiteralOccurrences,
        int EventPathLiteralXrefCount,
        int EventPathMatchedRows,
        int EventPathDistinctMatchedPaths,
        int EventRows,
        int BankCandidatePoolSize,
        int EventExactPcmMatchRows,
        int EventHashesWithMatch,
        int UniqueEvtAbkcSampleBindingProvenRows,
        int EventRowsWithoutStaticEventXref,
        int StaticEventContextTargetPoolSize,
        int EventStaticCandidateModelSumBefore,
        int EventStaticCandidateModelSumAfter,
        int EventStaticContextReducedRows,
        int UniqueEventStaticSelectorProvenRows,
        int ValidEventRows,
        string Status,
        string SemanticBinding,
        string DirectSelectionBinding,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct OriginalPrimitiveStreamTopologyEvidence(
        bool Valid,
        int ModelPackageCandidates,
        int ModelPackagesLoaded,
        int ObjectCandidates,
        int ObjectsLoaded,
        int ObjectsWithValidatedStreams,
        int PrimitiveRows,
        int StreamRows,
        int PrimitiveRowsWithOrdinalMatchedStreams,
        int StreamRowsWithOrdinalMatch,
        int StreamRowsWithStrideMatch,
        int StreamRowsWithValidPayloadRange,
        int CompletePrimitiveRows,
        int ObjectsWithNonOverlappingPayloadRanges,
        int ReversedSlotMatchRows,
        int ShiftedOrdinalMatchRows,
        int RejectedPackages,
        int RejectedObjects,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectIndexedCfgEvidence(
        bool Valid,
        int CandidateTargets,
        int AnalyzedTargets,
        int TargetsWithReachableCfg,
        int TargetsWithReachableRet,
        int TotalLinearInstructions,
        int TotalReachableInstructions,
        int TotalBasicBlocks,
        int TotalConditionalBranches,
        int TotalUnconditionalBranches,
        int TotalInternalBranchTargets,
        int TotalOutOfWindowBranchTargets,
        int TotalDirectCalls,
        int TotalUniqueDirectCallTargets,
        int DistinctDirectCallTargets,
        int TotalIndirectControlTransfers,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectIndexedCfgCallTargetEvidence(
        bool Valid,
        int CfgDirectCallOccurrences,
        int CfgUniqueDirectCallTargetsSum,
        int DistinctDirectCallTargets,
        int MappedExecutableTargets,
        int DisassembledTargets,
        int RetTerminatedTargets,
        int PrologueLikeTargets,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectIndexedCfgCallSiteEvidence(
        bool Valid,
        int SourceCfgCandidates,
        int DirectCallOccurrences,
        int DistinctCallTargets,
        int CandidateTargetsWithCallSites,
        int CallSitesWithPushEdxBeforeLea,
        int CallSitesWithLeaEcxBaseBeforeCall,
        int CallSitesWithRetImm4AfterCall,
        int DistinctLeaDisplacements,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectIndexedCfgCallTargetCfgEvidence(
        bool Valid,
        int CandidateTargets,
        int AnalyzedTargets,
        int TargetsWithReachableCfg,
        int TargetsWithReachableRet,
        int TotalLinearInstructions,
        int TotalReachableInstructions,
        int TotalBasicBlocks,
        int TotalConditionalBranches,
        int TotalUnconditionalBranches,
        int TotalInternalBranchTargets,
        int TotalOutOfWindowBranchTargets,
        int TotalDirectCalls,
        int TotalUniqueDirectCallTargets,
        int DistinctDirectCallTargets,
        int TotalIndirectControlTransfers,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence(
        bool Valid,
        int ParentTargetRows,
        int ParentDirectCallOccurrences,
        int DistinctNestedTargets,
        int AnalyzedTargets,
        int TargetsWithReachableCfg,
        int TargetsWithReachableRet,
        int TotalLinearInstructions,
        int TotalReachableInstructions,
        int TotalBasicBlocks,
        int TotalConditionalBranches,
        int TotalUnconditionalBranches,
        int TotalInternalBranchTargets,
        int TotalOutOfWindowBranchTargets,
        int TotalDirectCalls,
        int TotalUniqueDirectCallTargets,
        int DistinctNestedDirectCallTargets,
        int TotalIndirectControlTransfers,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectIndexedCfgNestedCallSiteEvidence(
        bool Valid,
        int ParentCfgTargetRows,
        int NestedCallTargetRows,
        int DistinctNestedCallTargets,
        int NestedCallSitesWithPushEdxBeforeLea,
        int NestedCallSitesWithLeaEcxBaseBeforeCall,
        int NestedCallSitesWithRetImm4AfterCall,
        int DistinctNestedLeaDisplacements,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectIndexedCfgCallSiteJoinEvidence(
        bool Valid,
        int IndexedTableTargetRows,
        int DistinctTableBases,
        int CallSiteRows,
        int JoinedCallSiteRows,
        int DistinctCallTargets,
        int DistinctTableEntryIndices,
        int TableEntryOccurrencesForJoinedCallSites,
        int DuplicateTableOccurrencesOverCallSites,
        int CallSitesWithCompleteLocalShape,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectIndexedCfgJoinEvidence(
        bool Valid,
        int CfgDirectCallTargets,
        int RenderDirectCallTargets,
        int RenderWindowTargets,
        int RenderCallGraphNodes,
        int CfgTargetsInRenderDirectCallTargets,
        int CfgTargetsInRenderWindowTargets,
        int CfgTargetsInRenderCallGraphNodes,
        string Status,
        string Provenance,
        string OriginalSemantics);

    private readonly record struct StaticRenderIndirectIndexedTargetJoinEvidence(
        bool Valid,
        int CandidateTargets,
        int DirectCallTargets,
        int RenderWindowTargets,
        int CandidateTargetsInDirectCallTargets,
        int CandidateTargetsInRenderWindowTargets,
        string Status,
        string Provenance,
        string OriginalSemantics);

    public const string KnownIsoSha256 =
        "F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2";
    public const long KnownIsoLength = 2_013_003_776;

    public static ProjectReadinessSummary Build(
        GameAssetCatalog catalog,
        string isoPath,
        string workspaceDirectory)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceDirectory);

        var isoFullPath = Path.GetFullPath(isoPath);
        var attributes = File.GetAttributes(isoFullPath);
        var length = new FileInfo(isoFullPath).Length;
        var sha256 = ComputeSha256(isoFullPath);
        var reportRoot = Path.GetFullPath(workspaceDirectory);
        var windowedFrameCapture = ReadWindowedFrameCapture(
            Path.Combine(reportRoot, "fifa_pc", "windowed-runtime-capture.ppm"));
        var staticRenderEvidence = ReadStaticRenderEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-static-summary.tsv"));
        var staticRenderTargetEvidence = ReadStaticRenderTargetEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-target-summary.tsv"));
        var staticRenderCfgEvidence = ReadStaticRenderCfgEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-cfg-summary.tsv"));
        var staticRenderCallTargetEvidence = ReadStaticRenderCallTargetEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-call-target-summary.tsv"));
        var staticRenderCallGraphEvidence = ReadStaticRenderCallGraphEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-call-graph-summary.tsv"));
        var staticRenderIndirectTransferEvidence = ReadStaticRenderIndirectTransferEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-transfer-summary.tsv"));
        var staticRenderIndirectReferenceEvidence = ReadStaticRenderIndirectReferenceEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-reference-summary.tsv"));
        var staticRenderIndirectReferenceSliceEvidence = ReadStaticRenderIndirectReferenceSliceEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-reference-slice-summary.tsv"));
        var staticRenderIndirectWindowJoinEvidence = ReadStaticRenderIndirectWindowJoinEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-window-join-summary.tsv"));
        var staticRenderIndirectIndexedTableEvidence = ReadStaticRenderIndirectIndexedTableEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-indexed-table-summary.tsv"));
        var staticRenderIndirectIndexedTargetEvidence = ReadStaticRenderIndirectIndexedTargetEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-indexed-target-summary.tsv"));
        var staticRenderIndirectIndexedAccessorEvidence = ReadStaticRenderIndirectIndexedAccessorEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-indexed-accessor-summary.tsv"));
        var candidateFetchAssociationEvidence = ReadCandidateFetchAssociationEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-candidate-fetch-association-summary.tsv"));
        var originalCandidateSlotProfileEvidence = ReadOriginalCandidateSlotProfileEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-candidate-slot-profile-control-summary.tsv"));
        var originalCandidateRawStreamIdentityEvidence = ReadOriginalCandidateRawStreamIdentityEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-candidate-raw-stream-identity-summary.tsv"));
        var originalCandidateSlotCategoryControlEvidence = ReadOriginalCandidateSlotCategoryControlEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-candidate-slot-category-control-summary.tsv"));
        var originalRenderChainEvidence = ReadOriginalRenderChainEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-render-chain-summary.tsv"));
        var originalRenderCommandDispatchEvidence = ReadStaticRenderCommandDispatchEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-render-command-dispatch-control-summary.tsv"));
        var originalVertexRecordAssociationEvidence = ReadOriginalVertexRecordAssociationEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-vertex-record-association-summary.tsv"));
        var originalVertexRecordAssociationControlEvidence = ReadOriginalVertexRecordAssociationControlEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-vertex-record-association-control-summary.tsv"));
        var originalVertexPayloadEvidence = ReadOriginalVertexPayloadEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-vertex-payload-summary.tsv"));
        var originalVertexPayloadControlEvidence = ReadOriginalVertexPayloadControlEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-vertex-payload-control-summary.tsv"));
        var originalNumericStreamAmbiguityEvidence = ReadOriginalNumericStreamAmbiguityEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-numeric-stream-ambiguity-control-summary.tsv"));
        var originalBoundsConsistentRecordEvidence = ReadOriginalBoundsConsistentRecordEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-bounds-consistent-record-control-summary.tsv"));
        var originalBoundsFetchSlotJoinEvidence = ReadOriginalBoundsFetchSlotJoinEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-bounds-fetch-slot-join-control-summary.tsv"));
        var originalNumericSlotRoleDiscriminationEvidence = ReadOriginalNumericSlotRoleDiscriminationEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-numeric-slot-role-discrimination-control-summary.tsv"));
        var originalStateNumericSlotCooccurrenceEvidence = ReadOriginalStateNumericSlotCooccurrenceEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-numeric-slot-cooccurrence-control-summary.tsv"));
        var originalCommandSlotRoleCooccurrenceEvidence = ReadOriginalCommandSlotRoleCooccurrenceEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-command-slot-role-cooccurrence-control-summary.tsv"));
        var originalRenderStateGeometryBoundsControlEvidence = ReadOriginalRenderStateGeometryBoundsControlEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-render-state-geometry-bounds-control-summary.tsv"));
        var originalStatePopulationControlEvidence = ReadOriginalStatePopulationControlEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-population-control-summary.tsv"));
        var originalStateTokenEvidence = ReadOriginalStateTokenEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-token-summary.tsv"));
        var originalStateCandidateAttributeControlEvidence = ReadOriginalStateCandidateAttributeControlEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-candidate-attribute-control-summary.tsv"));
        var candidateAttributeTextureCompatibilityEvidence = ReadCandidateAttributeTextureCompatibilityEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-candidate-attribute-texture-compatibility-control-summary.tsv"));
        var originalShapenameCandidateAssetControlEvidence = ReadOriginalShapenameCandidateAssetControlEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-shapename-candidate-asset-control-summary.tsv"));
        var originalStateCandidatePrimitiveShapeControlEvidence = ReadOriginalStateCandidatePrimitiveShapeControlEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-candidate-primitive-shape-control-summary.tsv"));
        var originalCandidateShapenamePrimitiveIndexControlEvidence = ReadOriginalCandidateShapenamePrimitiveIndexControlEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-candidate-shapename-primitive-index-control-summary.tsv"));
        var originalCandidatePrimitiveFetchControlEvidence = ReadOriginalCandidatePrimitiveFetchControlEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-candidate-primitive-fetch-control-summary.tsv"));
        var originalGeometryDrawClassificationEvidence = ReadOriginalGeometryDrawClassificationEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-geometry-draw-classification-control-summary.tsv"));
        var originalStateCandidateMaterialAmbiguityEvidence = ReadOriginalStateCandidateMaterialAmbiguityEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-candidate-material-ambiguity-control-summary.tsv"));
        var originalCandidateStreamRecordControlEvidence = ReadOriginalCandidateStreamRecordControlEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-candidate-stream-record-control-summary.tsv"));
        var originalCandidateGeometryStreamControlEvidence = ReadOriginalCandidateGeometryStreamControlEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-candidate-geometry-stream-control-summary.tsv"));
        var originalCandidateGeometryPayloadIdentityEvidence = ReadOriginalCandidateGeometryPayloadIdentityEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-candidate-geometry-payload-identity-control-summary.tsv"));
        var originalPrimitiveStreamTopologyEvidence = ReadOriginalPrimitiveStreamTopologyEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-primitive-stream-topology-summary.tsv"));
        var originalPrimitiveStreamOpaqueDiscriminationEvidence = ReadOriginalPrimitiveStreamOpaqueDiscriminationEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-primitive-stream-opaque-discrimination-summary.tsv"),
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-primitive-stream-opaque-discrimination-control-summary.tsv"));
        var originalPrimitiveStreamCommandPayloadIdentityEvidence =
            ReadOriginalPrimitiveStreamCommandPayloadIdentityEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-primitive-stream-command-payload-identity-summary.tsv"),
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-primitive-stream-command-payload-identity-control-summary.tsv"));
        var originalPrimitiveStreamCommandDescriptorAdjacencyEvidence =
            ReadOriginalPrimitiveStreamCommandDescriptorAdjacencyEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-primitive-stream-command-descriptor-adjacency-summary.tsv"),
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-primitive-stream-command-descriptor-adjacency-control-summary.tsv"));
        var originalStateFetchAdjacencyControlEvidence =
            ReadOriginalStateFetchAdjacencyControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-fetch-adjacency-control-summary.tsv"));
        var originalPrimitiveFetchTopologyControlEvidence =
            ReadOriginalPrimitiveFetchTopologyControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-primitive-fetch-topology-control-summary.tsv"));
        var originalCommandPayloadRegionControlEvidence =
            ReadOriginalCommandPayloadRegionControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-command-payload-region-control-summary.tsv"));
        var originalRenderPayloadObjectJoinEvidence = ReadOriginalRenderPayloadObjectJoinEvidence(
            Path.Combine(reportRoot, "fifa_pc", "eagl-original-render-payload-object-join-summary.tsv"));
        var originalStateGeometryPartitionControlEvidence =
            ReadOriginalStateGeometryPartitionControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-geometry-partition-control-summary.tsv"));
        var originalStateGeometryBindingAmbiguityControlEvidence =
            ReadOriginalStateGeometryBindingAmbiguityControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-geometry-binding-ambiguity-control-summary.tsv"));
        var originalStateGeometryGlobalAssignmentControlEvidence =
            ReadOriginalStateGeometryGlobalAssignmentControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-geometry-global-assignment-control-summary.tsv"));
        var originalStateGeometryUvMaterialGlobalAssignmentControlEvidence =
            ReadOriginalStateGeometryUvMaterialGlobalAssignmentControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-geometry-uv-material-global-assignment-control-summary.tsv"));
        var originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence =
            ReadOriginalStateGeometryCandidateRenderGlobalAssignmentControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-geometry-candidate-render-global-assignment-control-summary.tsv"));
        var originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence =
            ReadOriginalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-geometry-payload-identity-global-assignment-control-summary.tsv"));
        var originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence =
            ReadOriginalStateGeometryDrawClassificationGlobalAssignmentControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-geometry-draw-classification-global-assignment-control-summary.tsv"));
        var originalStateUidAmbiguityControlEvidence =
            ReadOriginalStateUidAmbiguityControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-uid-ambiguity-control-summary.tsv"));
        var originalStateUidCommandHashAmbiguityControlEvidence =
            ReadOriginalStateUidCommandHashAmbiguityControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-uid-command-hash-ambiguity-control-summary.tsv"));
        var originalStateProfileDiscriminationControlEvidence =
            ReadOriginalStateProfileDiscriminationControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-profile-discrimination-control-summary.tsv"));
        var originalStateSlot1CandidateUvAmbiguityEvidence =
            ReadOriginalStateSlot1CandidateUvAmbiguityEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-slot1-candidate-uv-ambiguity-control-summary.tsv"));
        var animationMorphPoseBankAmbiguityControlEvidence =
            ReadAnimationMorphPoseBankAmbiguityControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "animation-morph-pose-bank-ambiguity-control-summary.tsv"));
        var animationCatalogPoseTimingGlobalAssignmentControlEvidence =
            ReadAnimationCatalogPoseTimingGlobalAssignmentControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "animation-catalog-pose-timing-global-assignment-control-summary.tsv"));
        var originalStateGeometryPayloadJoinControlEvidence =
            ReadOriginalStateGeometryPayloadJoinControlEvidence(
                Path.Combine(reportRoot, "fifa_pc", "eagl-original-state-geometry-payload-join-control-summary.tsv"));
        var staticRenderIndirectIndexedCfgEvidence = ReadStaticRenderIndirectIndexedCfgEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-indexed-cfg-summary.tsv"));
        var staticRenderIndirectIndexedCfgCallTargetEvidence = ReadStaticRenderIndirectIndexedCfgCallTargetEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-indexed-cfg-call-target-summary.tsv"));
        var staticRenderIndirectIndexedCfgCallTargetCfgEvidence = ReadStaticRenderIndirectIndexedCfgCallTargetCfgEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-indexed-cfg-call-target-cfg-summary.tsv"));
        var staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence = ReadStaticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-indexed-cfg-nested-call-target-cfg-summary.tsv"));
        var staticRenderIndirectIndexedCfgNestedCallSiteEvidence = ReadStaticRenderIndirectIndexedCfgNestedCallSiteEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-indexed-cfg-nested-call-site-summary.tsv"));
        var staticRenderIndirectIndexedCfgCallSiteEvidence = ReadStaticRenderIndirectIndexedCfgCallSiteEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-indexed-cfg-call-site-summary.tsv"));
        var staticRenderIndirectIndexedCfgCallSiteJoinEvidence = ReadStaticRenderIndirectIndexedCfgCallSiteJoinEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-indexed-cfg-call-site-join-summary.tsv"));
        var staticRenderIndirectIndexedCfgJoinEvidence = ReadStaticRenderIndirectIndexedCfgJoinEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-indexed-cfg-join-summary.tsv"));
        var staticRenderIndirectIndexedTargetJoinEvidence = ReadStaticRenderIndirectIndexedTargetJoinEvidence(
            Path.Combine(reportRoot, "fifa_pc", "xbe-render-indirect-indexed-target-join-summary.tsv"));

        var stateCorrespondence = ReadTsvRows(
            Path.Combine(reportRoot, "fifa_pc", "eagl-candidate-state-correspondence.tsv"));
        var stateGeometryAdjacency = ReadTsvRows(
            Path.Combine(reportRoot, "fifa_pc", "eagl-state-geometry-adjacency.tsv"));
        var stateGeometryAdjacencyVerified = stateGeometryAdjacency.Count(row =>
            string.Equals(Get(row, "status"), "verified-control", StringComparison.OrdinalIgnoreCase));
        var samePrimitiveIndexPairs = stateGeometryAdjacency.Sum(row =>
            int.TryParse(Get(row, "same_primitive_index_pairs"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
                ? count
                : 0);
        var adjacentPrimitivePairs = stateGeometryAdjacency.Sum(row =>
            int.TryParse(Get(row, "adjacent_primitive_pairs"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
                ? count
                : 0);
        var sameLayerPairs = stateGeometryAdjacency.Sum(row =>
            int.TryParse(Get(row, "same_layer_pairs"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
                ? count
                : 0);
        var stateGeometryProfiles = ReadTsvRows(
            Path.Combine(reportRoot, "fifa_pc", "eagl-state-geometry-profile-audit.tsv"));
        var stateGeometryProfilesVerified = stateGeometryProfiles.Count(row =>
            string.Equals(Get(row, "status"), "opaque-profile-control", StringComparison.OrdinalIgnoreCase));
        var exactCommandShapePairs = stateGeometryProfiles.Count(row =>
            string.Equals(Get(row, "exact_command_shape"), "True", StringComparison.OrdinalIgnoreCase));
        var uniqueStateProfileRows = stateGeometryProfiles.Count(row =>
            int.TryParse(Get(row, "state_profile_tie_count"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) &&
            count == 1);
        var distinctGeometryCommandShapes = stateGeometryProfiles
            .Select(row => Get(row, "geometry_command_shape_sha256"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Count();
        var distinctStateCommandShapes = stateGeometryProfiles
            .Select(row => Get(row, "state_command_shape_sha256"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Count();
        var stateGeometryOrder = ReadTsvRows(
            Path.Combine(reportRoot, "fifa_pc", "eagl-state-geometry-order-audit.tsv"));
        var stateGeometryOrderVerified = stateGeometryOrder.Count(row =>
            string.Equals(Get(row, "status"), "ordered-index-control", StringComparison.OrdinalIgnoreCase));
        var stateGeometryOrderNonEmpty = stateGeometryOrder.Count(row =>
            int.TryParse(Get(row, "state_primitive_count"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) &&
            count > 0);
        var stateGeometryOrderPartitioned = stateGeometryOrder
            .Where(row => string.Equals(Get(row, "status"), "ordered-index-control", StringComparison.OrdinalIgnoreCase))
            .Sum(row =>
                int.TryParse(Get(row, "state_primitive_count"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
                    ? count
                    : 0);
        var candidateRuntimeRender = ReadTsvRows(
            Path.Combine(reportRoot, "fifa_pc", "eagl-material-runtime-batch.tsv"));
        var candidateRuntimeVerified = candidateRuntimeRender.Count(row =>
            string.Equals(Get(row, "status"), "verified-candidate", StringComparison.OrdinalIgnoreCase));
        var candidateRuntimeRejected = candidateRuntimeRender.Count(row =>
            string.Equals(Get(row, "status"), "rejected-candidate", StringComparison.OrdinalIgnoreCase));
        var candidateRuntimeChecksums = candidateRuntimeRender
            .Select(row => Get(row, "frame_checksum"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var candidateRuntimeHomeSlots = candidateRuntimeRender.Count(row =>
            !string.IsNullOrWhiteSpace(Get(row, "candidate_home_slot_checksum")));
        var candidateRuntimeAwaySlots = candidateRuntimeRender.Count(row =>
            !string.IsNullOrWhiteSpace(Get(row, "candidate_away_slot_checksum")));
        var candidateRuntimeBothSlots = candidateRuntimeRender.Count(row =>
            !string.IsNullOrWhiteSpace(Get(row, "candidate_both_slot_checksum")));
        var candidateSourceChainSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "eagl-candidate-asset-provenance-audit-summary.tsv");
        var candidateSourceChainSummary = ReadKeyValueTsv(
            candidateSourceChainSummaryPath,
            "field\tvalue");
        var candidateSourceChainVerified =
            HasHeader(candidateSourceChainSummaryPath, "field\tvalue") &&
            string.Equals(Get(candidateSourceChainSummary, "status"), "verified-candidate-source-chain", StringComparison.Ordinal) &&
            string.Equals(Get(candidateSourceChainSummary, "candidate_provenance"), EaglCandidateAssetProvenanceAudit.CandidateProvenance, StringComparison.Ordinal) &&
            string.Equals(Get(candidateSourceChainSummary, "original_semantics"), "not-claimed", StringComparison.Ordinal) &&
            ReadInt(candidateSourceChainSummary, "candidate_count") == catalog.CandidateMaterials.Count &&
            ReadInt(candidateSourceChainSummary, "source_chain_verified") == catalog.CandidateMaterials.Count &&
            ReadInt(candidateSourceChainSummary, "decoded_pixel_matches") == catalog.CandidateMaterials.Count;
        var candidateMeshStateCrosscheckVerified =
            candidateSourceChainVerified &&
            ReadInt(candidateSourceChainSummary, "candidate_structural_verified") == catalog.CandidateMaterials.Count &&
            ReadInt(candidateSourceChainSummary, "model_object_entries_read") == catalog.CandidateMaterials.Count &&
            ReadInt(candidateSourceChainSummary, "model_companion_entries_read") == catalog.CandidateMaterials.Count &&
            ReadInt(candidateSourceChainSummary, "candidate_bounds_valid") == catalog.CandidateMaterials.Count &&
            ReadInt(candidateSourceChainSummary, "candidate_geometry_finite_and_indexed") == catalog.CandidateMaterials.Count &&
            ReadInt(candidateSourceChainSummary, "candidate_primitive_shape_state_rows") == catalog.CandidateMaterials.Count &&
            ReadInt(candidateSourceChainSummary, "candidate_geometry_streams_complete") == catalog.CandidateMaterials.Count &&
            ReadInt(candidateSourceChainSummary, "candidate_shape_state_streams_complete") == catalog.CandidateMaterials.Count;
        var candidateIntegratedRuntimeAuditPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "candidate-integrated-runtime-audit.tsv");
        var candidateIntegratedRuntimeAuditRows = ReadTsvRows(candidateIntegratedRuntimeAuditPath);
        var candidateIntegratedRuntimeAudit = candidateIntegratedRuntimeAuditRows.FirstOrDefault(row =>
            string.Equals(Get(row, "kind"), "scenario", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Get(row, "name"), "candidate-full-stack", StringComparison.OrdinalIgnoreCase));
        var candidateIntegratedRuntimeAuditVerified =
            HasHeader(candidateIntegratedRuntimeAuditPath, CandidateIntegratedRuntimeAudit.Header) &&
            candidateIntegratedRuntimeAudit is not null &&
            string.Equals(Get(candidateIntegratedRuntimeAudit, "status"), "verified-candidate", StringComparison.Ordinal) &&
            string.Equals(Get(candidateIntegratedRuntimeAudit, "candidate_flags_valid"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(candidateIntegratedRuntimeAudit, "provenance_valid"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(candidateIntegratedRuntimeAudit, "candidate_asset_provenance_valid"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(candidateIntegratedRuntimeAudit, "candidate_mesh_state_crosscheck_valid"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(candidateIntegratedRuntimeAudit, "candidate_geometry_streams_complete"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(candidateIntegratedRuntimeAudit, "candidate_shape_state_streams_complete"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(candidateIntegratedRuntimeAudit, "integration_reports_match"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(candidateIntegratedRuntimeAudit, "trace_files_valid"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(candidateIntegratedRuntimeAudit, "replay_matched"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(candidateIntegratedRuntimeAudit, "frame_output_valid"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(candidateIntegratedRuntimeAudit, "original_semantics"), "not-claimed", StringComparison.Ordinal);
        var candidateRenderInfluence = ReadTsvRows(
            Path.Combine(reportRoot, "fifa_pc", "candidate-render-influence-audit.tsv"));
        var candidateRenderInfluenceRows = candidateRenderInfluence
            .Where(row => string.Equals(Get(row, "kind"), "candidate", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var candidateRenderInfluenceVerified = candidateRenderInfluenceRows.Count(row =>
            string.Equals(Get(row, "status"), "verified-candidate-raster-influence", StringComparison.OrdinalIgnoreCase));
        var candidateRenderInfluenceTextureRows = candidateRenderInfluenceRows.Count(row =>
            string.Equals(Get(row, "candidate_texture_influences_raster"), "True", StringComparison.OrdinalIgnoreCase));
        var candidateRenderInfluenceIsolatedRows = candidateRenderInfluenceRows.Count(row =>
            string.Equals(Get(row, "isolated_texture_influences_raster"), "True", StringComparison.OrdinalIgnoreCase));
        var candidateRenderInfluenceGeometryRows = candidateRenderInfluenceRows.Count(row =>
            string.Equals(Get(row, "geometry_finite_and_indexed"), "True", StringComparison.OrdinalIgnoreCase));
        var candidateAttributeRasterInfluencePath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "eagl-candidate-attribute-raster-influence-audit.tsv");
        var candidateAttributeRasterInfluenceRows = ReadTsvRows(candidateAttributeRasterInfluencePath);
        var candidateAttributeRasterInfluenceCandidateRows = candidateAttributeRasterInfluenceRows
            .Where(row => string.Equals(Get(row, "kind"), "candidate", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var candidateAttributeRasterInfluenceSummary = candidateAttributeRasterInfluenceRows.FirstOrDefault(row =>
            string.Equals(Get(row, "kind"), "summary", StringComparison.OrdinalIgnoreCase));
        var candidateAttributeRasterInfluenceVerifiedRows = candidateAttributeRasterInfluenceCandidateRows.Count(row =>
            string.Equals(Get(row, "status"), "verified-candidate-attribute-raster-influence", StringComparison.OrdinalIgnoreCase));
        var candidateAttributeRasterInfluenceVerified =
            HasHeader(candidateAttributeRasterInfluencePath, CandidateAttributeRasterInfluenceAudit.Header) &&
            candidateAttributeRasterInfluenceSummary is not null &&
            candidateAttributeRasterInfluenceCandidateRows.Length == 24 &&
            candidateAttributeRasterInfluenceVerifiedRows == 24 &&
            ReadInt(candidateAttributeRasterInfluenceSummary, "geometry_finite_and_indexed") == 24 &&
            ReadInt(candidateAttributeRasterInfluenceSummary, "attribute_cardinality_valid") == 24 &&
            ReadInt(candidateAttributeRasterInfluenceSummary, "attribute_perturbs_raster") == 24 &&
            string.Equals(Get(candidateAttributeRasterInfluenceSummary, "status"), "verified-candidate-attribute-raster-influence", StringComparison.Ordinal) &&
            string.Equals(Get(candidateAttributeRasterInfluenceSummary, "original_semantics"), "not-claimed", StringComparison.Ordinal);
        var roundTracePath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-script-round-trace.tsv");
        var roundReplayPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-script-round-replayed.tsv");
        var roundTraceRows = ReadTsvRows(roundTracePath);
        var roundReplayRows = ReadTsvRows(roundReplayPath);
        var authoredRoundTraceHasGoal = roundTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "GOAL_HOME", StringComparison.Ordinal) ||
            string.Equals(Get(row, "action"), "GOAL_AWAY", StringComparison.Ordinal));
        var authoredRoundTraceHasRestart = roundTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "RESTART", StringComparison.Ordinal));
        var authoredRoundTraceReplayVerified =
            roundTraceRows.Count == 7 &&
            roundTraceRows.Count == roundReplayRows.Count &&
            authoredRoundTraceHasGoal &&
            authoredRoundTraceHasRestart &&
            File.Exists(roundTracePath) &&
            File.Exists(roundReplayPath) &&
            string.Equals(
                ComputeSha256(roundTracePath),
                ComputeSha256(roundReplayPath),
                StringComparison.OrdinalIgnoreCase);
        var gamebreakerTracePath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-gamebreaker-match-trace.tsv");
        var gamebreakerReplayPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-gamebreaker-match-replayed.tsv");
        var gamebreakerTraceRows = ReadTsvRows(gamebreakerTracePath);
        var gamebreakerReplayRows = ReadTsvRows(gamebreakerReplayPath);
        var authoredGamebreakerTraceHasGoal = gamebreakerTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "GOAL_HOME", StringComparison.Ordinal));
        var authoredGamebreakerTraceHasAction = gamebreakerTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "GAMEBREAKER", StringComparison.Ordinal));
        var authoredGamebreakerTraceReplayVerified =
            gamebreakerTraceRows.Count == 7 &&
            gamebreakerTraceRows.Count == gamebreakerReplayRows.Count &&
            authoredGamebreakerTraceHasGoal &&
            authoredGamebreakerTraceHasAction &&
            File.Exists(gamebreakerTracePath) &&
            File.Exists(gamebreakerReplayPath) &&
            string.Equals(
                ComputeSha256(gamebreakerTracePath),
                ComputeSha256(gamebreakerReplayPath),
                StringComparison.OrdinalIgnoreCase);
        var juggleTracePath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-juggle-match-trace.tsv");
        var juggleReplayPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-juggle-match-replayed.tsv");
        var juggleTraceRows = ReadTsvRows(juggleTracePath);
        var juggleTraceHasGoal = juggleTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "GOAL_HOME", StringComparison.Ordinal));
        var juggleTraceHasAction = juggleTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "JUGGLE", StringComparison.Ordinal));
        var authoredJuggleTraceReplayVerified =
            juggleTraceRows.Count == 7 &&
            juggleTraceRows.Count == ReadTsvRows(juggleReplayPath).Count &&
            juggleTraceHasGoal &&
            juggleTraceHasAction &&
            File.Exists(juggleTracePath) &&
            File.Exists(juggleReplayPath) &&
            string.Equals(
                ComputeSha256(juggleTracePath),
                ComputeSha256(juggleReplayPath),
                StringComparison.OrdinalIgnoreCase);
        var pannaTracePath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-panna-match-trace.tsv");
        var pannaReplayPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-panna-match-replayed.tsv");
        var pannaTraceRows = ReadTsvRows(pannaTracePath);
        var pannaReplayRows = ReadTsvRows(pannaReplayPath);
        var authoredPannaTraceHasAction = pannaTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "PANNA", StringComparison.Ordinal));
        var authoredPannaTraceHasFullTime = pannaTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "FULL_TIME", StringComparison.Ordinal));
        var authoredPannaTraceReplayVerified =
            pannaTraceRows.Count == 3 &&
            pannaTraceRows.Count == pannaReplayRows.Count &&
            authoredPannaTraceHasAction &&
            authoredPannaTraceHasFullTime &&
            File.Exists(pannaTracePath) &&
            File.Exists(pannaReplayPath) &&
            string.Equals(
                ComputeSha256(pannaTracePath),
                ComputeSha256(pannaReplayPath),
                StringComparison.OrdinalIgnoreCase);
        var fullTimeTracePath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-full-time-match-trace.tsv");
        var fullTimeReplayPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-full-time-match-replayed.tsv");
        var fullTimeTraceRows = ReadTsvRows(fullTimeTracePath);
        var fullTimeReplayRows = ReadTsvRows(fullTimeReplayPath);
        var authoredFullTimeTraceHasAction = fullTimeTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "FULL_TIME", StringComparison.Ordinal));
        var authoredFullTimeTraceReplayVerified =
            fullTimeTraceRows.Count == 2 &&
            fullTimeTraceRows.Count == fullTimeReplayRows.Count &&
            authoredFullTimeTraceHasAction &&
            File.Exists(fullTimeTracePath) &&
            File.Exists(fullTimeReplayPath) &&
            string.Equals(
                ComputeSha256(fullTimeTracePath),
                ComputeSha256(fullTimeReplayPath),
                StringComparison.OrdinalIgnoreCase);
        var stealTracePath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-steal-match-trace.tsv");
        var stealReplayPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-steal-match-replayed.tsv");
        var stealTraceRows = ReadTsvRows(stealTracePath);
        var stealReplayRows = ReadTsvRows(stealReplayPath);
        var authoredStealTraceHasAction = stealTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "STEAL", StringComparison.Ordinal));
        var authoredStealTraceReplayVerified =
            stealTraceRows.Count == 2 &&
            stealTraceRows.Count == stealReplayRows.Count &&
            authoredStealTraceHasAction &&
            File.Exists(stealTracePath) &&
            File.Exists(stealReplayPath) &&
            string.Equals(
                ComputeSha256(stealTracePath),
                ComputeSha256(stealReplayPath),
                StringComparison.OrdinalIgnoreCase);
        var tackleTracePath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-tackle-match-trace.tsv");
        var tackleReplayPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-tackle-match-replayed.tsv");
        var tackleTraceRows = ReadTsvRows(tackleTracePath);
        var tackleReplayRows = ReadTsvRows(tackleReplayPath);
        var authoredTackleTraceHasAction = tackleTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "TACKLE", StringComparison.Ordinal));
        var authoredTackleTraceReplayVerified =
            tackleTraceRows.Count == 5 &&
            tackleTraceRows.Count == tackleReplayRows.Count &&
            authoredTackleTraceHasAction &&
            HasHeader(tackleTracePath, MatchTraceSerializer.Header) &&
            HasHeader(tackleReplayPath, MatchTraceSerializer.Header) &&
            File.Exists(tackleTracePath) &&
            File.Exists(tackleReplayPath) &&
            string.Equals(
                ComputeSha256(tackleTracePath),
                ComputeSha256(tackleReplayPath),
                StringComparison.OrdinalIgnoreCase);
        var aimedTracePath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-aimed-match-trace.tsv");
        var aimedReplayPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-aimed-match-replayed.tsv");
        var aimedTraceRows = ReadTsvRows(aimedTracePath);
        var aimedReplayRows = ReadTsvRows(aimedReplayPath);
        var authoredAimedTraceHasShot = aimedTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "SHOT", StringComparison.Ordinal));
        var authoredAimedTraceHasGoal = aimedTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "GOAL_HOME", StringComparison.Ordinal) ||
            string.Equals(Get(row, "action"), "GOAL_AWAY", StringComparison.Ordinal));
        var authoredAimedTraceReplayVerified =
            aimedTraceRows.Count == 6 &&
            aimedTraceRows.Count == aimedReplayRows.Count &&
            authoredAimedTraceHasShot &&
            authoredAimedTraceHasGoal &&
            HasHeader(aimedTracePath, MatchTraceSerializer.Header) &&
            HasHeader(aimedReplayPath, MatchTraceSerializer.Header) &&
            File.Exists(aimedTracePath) &&
            File.Exists(aimedReplayPath) &&
            string.Equals(
                ComputeSha256(aimedTracePath),
                ComputeSha256(aimedReplayPath),
                StringComparison.OrdinalIgnoreCase);
        var authoredAimedInputTracePath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-aimed-input-trace.tsv");
        var authoredAimedInputReplayPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-aimed-input-replayed.tsv");
        var legacyAimedInputTracePath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-clean-room-aimed-input-trace.tsv");
        var legacyAimedInputReplayPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-clean-room-aimed-input-replayed.tsv");
        var aimedInputTracePath = File.Exists(authoredAimedInputTracePath)
            ? authoredAimedInputTracePath
            : legacyAimedInputTracePath;
        var aimedInputReplayPath = File.Exists(authoredAimedInputReplayPath)
            ? authoredAimedInputReplayPath
            : legacyAimedInputReplayPath;
        var aimedInputTraceRows = ReadTsvRows(aimedInputTracePath);
        var aimedInputReplayRows = ReadTsvRows(aimedInputReplayPath);
        var aimedInputShotRow = aimedInputTraceRows.FirstOrDefault(row =>
            string.Equals(Get(row, "frame"), "67", StringComparison.Ordinal));
        var authoredAimedInputTraceHasVerticalShot =
            aimedInputShotRow is not null &&
            string.Equals(Get(aimedInputShotRow, "move_y"), "0.75", StringComparison.Ordinal) &&
            string.Equals(Get(aimedInputShotRow, "shoot"), "True", StringComparison.Ordinal);
        var authoredAimedInputTraceReplayVerified =
            aimedInputTraceRows.Count == 80 &&
            aimedInputTraceRows.Count == aimedInputReplayRows.Count &&
            authoredAimedInputTraceHasVerticalShot &&
            HasHeader(aimedInputTracePath, CleanRoomInputTraceSerializer.Header) &&
            HasHeader(aimedInputReplayPath, CleanRoomInputTraceSerializer.Header) &&
            File.Exists(aimedInputTracePath) &&
            File.Exists(aimedInputReplayPath) &&
            string.Equals(
                ComputeSha256(aimedInputTracePath),
                ComputeSha256(aimedInputReplayPath),
                StringComparison.OrdinalIgnoreCase);
        var authoredAudioTracePath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-full-stack-audio-trace.tsv");
        var authoredAudioReplayPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-full-stack-audio-replayed.tsv");
        var authoredAudioTraceRows = ReadTsvRows(authoredAudioTracePath);
        var authoredAudioReplayRows = ReadTsvRows(authoredAudioReplayPath);
        var authoredAudioTraceHasKickoff = authoredAudioTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "KICKOFF", StringComparison.Ordinal) &&
            string.Equals(Get(row, "candidate"), "intro", StringComparison.Ordinal));
        var authoredAudioTraceAllPlayed = authoredAudioTraceRows.Count > 0 &&
            authoredAudioTraceRows.All(row =>
                string.Equals(Get(row, "route"), "played-clean-room-candidate", StringComparison.Ordinal));
        var authoredAudioTraceReplayVerified =
            authoredAudioTraceRows.Count == 8 &&
            authoredAudioTraceRows.Count == authoredAudioReplayRows.Count &&
            authoredAudioTraceHasKickoff &&
            authoredAudioTraceAllPlayed &&
            HasHeader(authoredAudioTracePath, GameplayAudioTraceSerializer.Header) &&
            HasHeader(authoredAudioReplayPath, GameplayAudioTraceSerializer.Header) &&
            File.Exists(authoredAudioTracePath) &&
            File.Exists(authoredAudioReplayPath) &&
            string.Equals(
                ComputeSha256(authoredAudioTracePath),
                ComputeSha256(authoredAudioReplayPath),
                StringComparison.OrdinalIgnoreCase);
        var authoredStealAudioTracePath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-steal-audio-trace.tsv");
        var authoredStealAudioReplayPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-steal-audio-replayed.tsv");
        var authoredStealAudioTraceRows = ReadTsvRows(authoredStealAudioTracePath);
        var authoredStealAudioReplayRows = ReadTsvRows(authoredStealAudioReplayPath);
        var authoredStealAudioTraceHasTackle = authoredStealAudioTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "STEAL", StringComparison.Ordinal) &&
            string.Equals(Get(row, "candidate"), "tackle", StringComparison.Ordinal));
        var authoredStealAudioTraceReplayVerified =
            authoredStealAudioTraceRows.Count == 2 &&
            authoredStealAudioTraceRows.Count == authoredStealAudioReplayRows.Count &&
            authoredStealAudioTraceHasTackle &&
            HasHeader(authoredStealAudioTracePath, GameplayAudioTraceSerializer.Header) &&
            HasHeader(authoredStealAudioReplayPath, GameplayAudioTraceSerializer.Header) &&
            File.Exists(authoredStealAudioTracePath) &&
            File.Exists(authoredStealAudioReplayPath) &&
            string.Equals(
                ComputeSha256(authoredStealAudioTracePath),
                ComputeSha256(authoredStealAudioReplayPath),
                StringComparison.OrdinalIgnoreCase);
        var authoredAimedAudioTracePath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-aimed-audio-trace.tsv");
        var authoredAimedAudioReplayPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-aimed-audio-replayed.tsv");
        var authoredAimedAudioTraceRows = ReadTsvRows(authoredAimedAudioTracePath);
        var authoredAimedAudioReplayRows = ReadTsvRows(authoredAimedAudioReplayPath);
        var authoredAimedAudioTraceHasShot = authoredAimedAudioTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "SHOT", StringComparison.Ordinal) &&
            string.Equals(Get(row, "candidate"), "attackgoal", StringComparison.Ordinal));
        var authoredAimedAudioTraceHasGoal = authoredAimedAudioTraceRows.Any(row =>
            string.Equals(Get(row, "action"), "GOAL_HOME", StringComparison.Ordinal) &&
            string.Equals(Get(row, "candidate"), "goal", StringComparison.Ordinal));
        var authoredAimedAudioTraceAllPlayed = authoredAimedAudioTraceRows.Count > 0 &&
            authoredAimedAudioTraceRows.All(row =>
                string.Equals(Get(row, "route"), "played-clean-room-candidate", StringComparison.Ordinal));
        var authoredAimedAudioTraceReplayVerified =
            authoredAimedAudioTraceRows.Count == 6 &&
            authoredAimedAudioTraceRows.Count == authoredAimedAudioReplayRows.Count &&
            authoredAimedAudioTraceHasShot &&
            authoredAimedAudioTraceHasGoal &&
            authoredAimedAudioTraceAllPlayed &&
            HasHeader(authoredAimedAudioTracePath, GameplayAudioTraceSerializer.Header) &&
            HasHeader(authoredAimedAudioReplayPath, GameplayAudioTraceSerializer.Header) &&
            File.Exists(authoredAimedAudioTracePath) &&
            File.Exists(authoredAimedAudioReplayPath) &&
            string.Equals(
                ComputeSha256(authoredAimedAudioTracePath),
                ComputeSha256(authoredAimedAudioReplayPath),
                StringComparison.OrdinalIgnoreCase);
        var integrationReportPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-full-stack-integration.tsv");
        var integrationReport = ReadKeyValueTsv(integrationReportPath);
        var authoredFullStackIntegrationVerified =
            HasHeader(integrationReportPath, CleanRoomIntegrationReport.Header) &&
            string.Equals(Get(integrationReport, "integration_scope"), "authored-clean-room-runtime", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "candidate_material_validated"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "candidate_home_enabled"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "candidate_away_enabled"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "opponent_ai_enabled"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "rendered_frames"), "140", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "input_samples"), "140", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "animation_samples"), "280", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "match_events"), "8", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "audio_events"), "8", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "audio_prepared"), "8", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "audio_played"), "8", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "audio_unmapped"), "0", StringComparison.Ordinal) &&
            ReadInt(integrationReport, "authored_scene_asset_frames") >= 140 &&
            string.Equals(
                Get(integrationReport, "authored_character_asset_manifest"),
                CleanRoomCharacterAssetManifest.Default.ManifestId,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(integrationReport, "authored_scene_asset_manifest"),
                CleanRoomSceneAssetManifest.Default.ManifestId,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(integrationReport, "authored_animation_asset_manifest"),
                CleanRoomAnimationAssetManifest.Default.ManifestId,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(integrationReport, "authored_audio_asset_manifest"),
                CleanRoomAudioAssetManifest.Default.ManifestId,
                StringComparison.Ordinal) &&
            ReadLong(integrationReport, "gameplay_audio_pcm_bytes") > 0 &&
            IsSha256Hex(Get(integrationReport, "gameplay_audio_pcm_sha256")) &&
            string.Equals(Get(integrationReport, "match_trace_written"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "input_trace_written"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "animation_trace_written"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "audio_trace_written"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "original_semantics"), "not-claimed", StringComparison.Ordinal);
        var authoredRenderedFrames = ReadInt(integrationReport, "rendered_frames");
        var authoredDistinctRenderChecksums = ReadInt(integrationReport, "distinct_render_checksums");
        var authoredSceneAssetFrames = ReadInt(integrationReport, "authored_scene_asset_frames");
        var authoredCharacterAssetManifest = Get(
            integrationReport,
            "authored_character_asset_manifest");
        var authoredMatchEvents = ReadInt(integrationReport, "match_events");
        var authoredAudioEvents = ReadInt(integrationReport, "audio_events");
        var authoredAudioAssetManifest = Get(
            integrationReport,
            "authored_audio_asset_manifest");
        var authoredAnimationSamples = ReadInt(integrationReport, "animation_samples");
        var authoredAnimationAssetManifest = Get(
            integrationReport,
            "authored_animation_asset_manifest");
        var authoredCandidateAssetRuntimeVerified =
            authoredFullStackIntegrationVerified &&
            string.Equals(Get(integrationReport, "candidate_material_validated"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "candidate_home_enabled"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "candidate_away_enabled"), "True", StringComparison.Ordinal) &&
            candidateRuntimeRejected == 0 &&
            candidateIntegratedRuntimeAuditVerified;
        var authoredRenderingRuntimeVerified =
            authoredFullStackIntegrationVerified &&
            authoredRenderedFrames >= 140 &&
            authoredDistinctRenderChecksums > 1;
        var authoredSceneAssetRuntimeVerified =
            authoredFullStackIntegrationVerified &&
            authoredSceneAssetFrames >= 140 &&
            string.Equals(
                Get(integrationReport, "authored_scene_asset_manifest"),
                CleanRoomSceneAssetManifest.Default.ManifestId,
                StringComparison.Ordinal);
        var authoredGameplayRuntimeVerified =
            authoredFullStackIntegrationVerified &&
            authoredMatchEvents >= 1 &&
            string.Equals(Get(integrationReport, "match_trace_written"), "True", StringComparison.Ordinal);
        var authoredAudioRuntimeVerified =
            authoredFullStackIntegrationVerified &&
            authoredAudioEvents >= 1 &&
            string.Equals(Get(integrationReport, "audio_prepared"), Get(integrationReport, "audio_events"), StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "audio_played"), Get(integrationReport, "audio_events"), StringComparison.Ordinal) &&
            string.Equals(Get(integrationReport, "audio_unmapped"), "0", StringComparison.Ordinal) &&
            ReadLong(integrationReport, "gameplay_audio_pcm_bytes") > 0 &&
            IsSha256Hex(Get(integrationReport, "gameplay_audio_pcm_sha256"));
        var authoredAudioAssetRuntimeVerified =
            authoredFullStackIntegrationVerified &&
            string.Equals(
                authoredAudioAssetManifest,
                CleanRoomAudioAssetManifest.Default.ManifestId,
                StringComparison.Ordinal);
        var authoredAnimationRuntimeVerified =
            authoredFullStackIntegrationVerified &&
            authoredAnimationSamples >= 2 &&
            string.Equals(Get(integrationReport, "animation_trace_written"), "True", StringComparison.Ordinal);
        var authoredAnimationAssetRuntimeVerified =
            authoredFullStackIntegrationVerified &&
            string.Equals(
                authoredAnimationAssetManifest,
                CleanRoomAnimationAssetManifest.Default.ManifestId,
                StringComparison.Ordinal);
        var authoredCharacterIntegrationReportPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-authored-character-integration.tsv");
        var authoredCharacterIntegrationReport = ReadKeyValueTsv(authoredCharacterIntegrationReportPath);
        var authoredCharacterAssetFrames = ReadInt(
            authoredCharacterIntegrationReport,
            "authored_character_asset_frames");
        var authoredCharacterRuntimeVerified =
            HasHeader(authoredCharacterIntegrationReportPath, CleanRoomIntegrationReport.Header) &&
            string.Equals(
                Get(authoredCharacterIntegrationReport, "authored_character_asset_manifest"),
                CleanRoomCharacterAssetManifest.Default.ManifestId,
                StringComparison.Ordinal) &&
            string.Equals(Get(authoredCharacterIntegrationReport, "integration_scope"), "authored-clean-room-runtime", StringComparison.Ordinal) &&
            string.Equals(Get(authoredCharacterIntegrationReport, "candidate_home_enabled"), "False", StringComparison.Ordinal) &&
            string.Equals(Get(authoredCharacterIntegrationReport, "candidate_away_enabled"), "False", StringComparison.Ordinal) &&
            authoredCharacterAssetFrames >= 1 &&
            authoredCharacterAssetFrames == ReadInt(authoredCharacterIntegrationReport, "rendered_frames") &&
            string.Equals(Get(authoredCharacterIntegrationReport, "original_semantics"), "not-claimed", StringComparison.Ordinal);
        var gameplayCoverageReportPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-coverage.tsv");
        var gameplayCoverageRows = ReadTsvRows(gameplayCoverageReportPath);
        var gameplayCoverageSummary = gameplayCoverageRows.FirstOrDefault(row =>
            string.Equals(Get(row, "kind"), "summary", StringComparison.Ordinal));
        var gameplayCoverageVerified =
            gameplayCoverageSummary is not null &&
            string.Equals(Get(gameplayCoverageSummary, "status"), "verified-authored", StringComparison.Ordinal) &&
            string.Equals(Get(gameplayCoverageSummary, "all_missing_actions"), string.Empty, StringComparison.Ordinal) &&
            string.Equals(Get(gameplayCoverageSummary, "original_semantics"), "not-claimed", StringComparison.Ordinal);
        var gameplayInvariantReportPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "clean-room-gameplay-invariants.tsv");
        var gameplayInvariantRows = ReadTsvRows(gameplayInvariantReportPath);
        var gameplayInvariantSummary = gameplayInvariantRows.FirstOrDefault(row =>
            string.Equals(Get(row, "kind"), "summary", StringComparison.Ordinal));
        var gameplayInvariantScenarioRows = gameplayInvariantRows.Where(row =>
            string.Equals(Get(row, "kind"), "scenario", StringComparison.Ordinal)).ToArray();
        var gameplayInvariantVerified =
            HasHeader(gameplayInvariantReportPath, CleanRoomGameplayInvariantAudit.Header) &&
            gameplayInvariantSummary is not null &&
            string.Equals(Get(gameplayInvariantSummary, "status"), "verified-authored", StringComparison.Ordinal) &&
            string.Equals(Get(gameplayInvariantSummary, "original_semantics"), "not-claimed", StringComparison.Ordinal) &&
            ReadInt(gameplayInvariantSummary, "scenario_count") > 0 &&
            ReadInt(gameplayInvariantSummary, "verified_scenario_count") == ReadInt(gameplayInvariantSummary, "scenario_count") &&
            ReadInt(gameplayInvariantSummary, "total_frames") > 0 &&
            ReadInt(gameplayInvariantSummary, "total_events") > 0 &&
            gameplayInvariantScenarioRows.Length == ReadInt(gameplayInvariantSummary, "scenario_count") &&
            gameplayInvariantScenarioRows.All(row =>
                string.Equals(Get(row, "status"), "verified-authored", StringComparison.Ordinal) &&
                string.Equals(Get(row, "finite_state"), "True", StringComparison.Ordinal) &&
                string.Equals(Get(row, "positions_bounded"), "True", StringComparison.Ordinal) &&
                string.Equals(Get(row, "ball_height_bounded"), "True", StringComparison.Ordinal) &&
                string.Equals(Get(row, "clock_monotonic"), "True", StringComparison.Ordinal) &&
                string.Equals(Get(row, "scores_monotonic"), "True", StringComparison.Ordinal) &&
                string.Equals(Get(row, "event_sequence_monotonic"), "True", StringComparison.Ordinal) &&
                string.Equals(Get(row, "end_condition_satisfied"), "True", StringComparison.Ordinal) &&
                string.Equals(Get(row, "required_actions_present"), "True", StringComparison.Ordinal) &&
                string.Equals(Get(row, "original_semantics"), "not-claimed", StringComparison.Ordinal));
        var authoredIntegratedCoverageReportPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "clean-room-integrated-coverage.tsv");
        var authoredIntegratedCoverageRows = ReadTsvRows(authoredIntegratedCoverageReportPath);
        var authoredIntegratedCoverageSummary = authoredIntegratedCoverageRows.FirstOrDefault(row =>
            string.Equals(Get(row, "kind"), "summary", StringComparison.Ordinal));
        var authoredIntegratedCoverageVerified =
            HasHeader(authoredIntegratedCoverageReportPath, CleanRoomIntegratedCoverageAudit.Header) &&
            authoredIntegratedCoverageSummary is not null &&
            string.Equals(Get(authoredIntegratedCoverageSummary, "status"), "verified-authored", StringComparison.Ordinal) &&
            string.Equals(Get(authoredIntegratedCoverageSummary, "original_semantics"), "not-claimed", StringComparison.Ordinal) &&
            ReadInt(authoredIntegratedCoverageSummary, "scenario_count") > 0 &&
            ReadInt(authoredIntegratedCoverageSummary, "verified_scenario_count") == ReadInt(authoredIntegratedCoverageSummary, "scenario_count") &&
            ReadInt(authoredIntegratedCoverageSummary, "total_rendered_frames") > 0 &&
            ReadInt(authoredIntegratedCoverageSummary, "total_audio_events") > 0 &&
            ReadInt(authoredIntegratedCoverageSummary, "total_animation_samples") > 0 &&
            authoredIntegratedCoverageRows.Count(row =>
                string.Equals(Get(row, "kind"), "scenario", StringComparison.Ordinal)) == ReadInt(authoredIntegratedCoverageSummary, "scenario_count") &&
            authoredIntegratedCoverageRows
                .Where(row => string.Equals(Get(row, "kind"), "scenario", StringComparison.Ordinal))
                .All(row =>
                    string.Equals(Get(row, "status"), "verified-authored", StringComparison.Ordinal) &&
                    string.Equals(Get(row, "integration_valid"), "True", StringComparison.Ordinal) &&
                    string.Equals(Get(row, "trace_files_valid"), "True", StringComparison.Ordinal) &&
                    string.Equals(Get(row, "replay_matched"), "True", StringComparison.Ordinal) &&
                    string.Equals(Get(row, "required_actions_present"), "True", StringComparison.Ordinal) &&
                    string.Equals(Get(row, "original_semantics"), "not-claimed", StringComparison.Ordinal));
        var cleanRoomGameplayGatePassed =
            authoredGameplayRuntimeVerified &&
            gameplayCoverageVerified &&
            gameplayInvariantVerified &&
            authoredIntegratedCoverageVerified;
        var cleanRoomAssetsGatePassed =
            authoredFullStackIntegrationVerified &&
            authoredSceneAssetRuntimeVerified &&
            authoredCharacterRuntimeVerified &&
            authoredIntegratedCoverageVerified;
        var cleanRoomRenderingGatePassed =
            authoredRenderingRuntimeVerified &&
            windowedFrameCapture.Valid &&
            authoredIntegratedCoverageVerified;
        var cleanRoomAnimationGatePassed =
            authoredAnimationRuntimeVerified &&
            authoredAnimationAssetRuntimeVerified &&
            authoredIntegratedCoverageVerified;
        var exactStatePairs = stateCorrespondence.Count(row =>
            string.Equals(Get(row, "exact_raw_bytes"), "True", StringComparison.OrdinalIgnoreCase));
        var sharedRelocationPairs = stateCorrespondence.Count(row =>
            int.TryParse(Get(row, "shared_relocation_symbol_count"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) &&
            count > 0);
        var shpxGeometryCorrelation = ReadTsvRows(
            Path.Combine(reportRoot, "fifa_pc", "morph-sibling-shpx-geometry-correlation.tsv"));
        var shpxGeometryBothVariable = shpxGeometryCorrelation.Count(row =>
            string.Equals(Get(row, "both_variable"), "True", StringComparison.OrdinalIgnoreCase));
        var shpxGeometryVariable = shpxGeometryCorrelation.Count(row =>
            string.Equals(Get(row, "candidate_geometry_variable"), "True", StringComparison.OrdinalIgnoreCase));
        var shpxVariable = shpxGeometryCorrelation.Count(row =>
            string.Equals(Get(row, "xsh_structure_variable"), "True", StringComparison.OrdinalIgnoreCase));
        var animationMorphRelocationProfile = ReadTsvRows(
            Path.Combine(reportRoot, "fifa_pc", "animation-morph-relocation-profile-audit-summary.tsv"));
        var animationMorphRelocationSummary = animationMorphRelocationProfile.FirstOrDefault();
        var animationMorphCatalogRows = ReadInt(animationMorphRelocationSummary, "morph_catalog_rows");
        var animationMorphLayouts = ReadInt(animationMorphRelocationSummary, "morph_layouts");
        var animationMorphParseValidLayouts = ReadInt(animationMorphRelocationSummary, "parse_valid_layouts");
        var animationMorphInvalidLayouts = ReadInt(animationMorphRelocationSummary, "invalid_layouts");
        var animationMorphBanks = ReadInt(animationMorphRelocationSummary, "animation_banks");
        var animationMorphContainingLayouts = ReadInt(animationMorphRelocationSummary, "layouts_with_containing_animation_bank");
        var animationMorphExactLayouts = ReadInt(animationMorphRelocationSummary, "layouts_with_exact_gap_set_bank");
        var animationMorphContainingPairs = ReadInt(animationMorphRelocationSummary, "containing_pairs");
        var animationMorphExactPairs = ReadInt(animationMorphRelocationSummary, "exact_gap_set_pairs");
        var animationMorphExactHistogramLayouts = ReadInt(animationMorphRelocationSummary, "layouts_with_exact_gap_histogram_bank");
        var animationMorphExactHistogramPairs = ReadInt(animationMorphRelocationSummary, "exact_gap_histogram_pairs");
        var animationPayloadNameSummaryRows = ReadTsvRows(
            Path.Combine(reportRoot, "fifa_pc", "animation-bank-catalog-payload-name-audit-summary.tsv"));
        var animationPayloadNameCatalogRows = animationPayloadNameSummaryRows.Sum(row =>
            ReadInt(row, "catalog_rows"));
        var animationPayloadNameTaggedRows = animationPayloadNameSummaryRows.Sum(row =>
            ReadInt(row, "tagged_rows"));
        var animationPayloadNameMissingRows = animationPayloadNameSummaryRows.Sum(row =>
            ReadInt(row, "missing_rows"));
        var animationPayloadNameAmbiguousRows = animationPayloadNameSummaryRows.Sum(row =>
            ReadInt(row, "ambiguous_rows"));
        var animationPayloadNameStringCount = animationPayloadNameSummaryRows.Sum(row =>
            ReadInt(row, "payload_string_count"));
        var animationPayloadNameVerified =
            animationPayloadNameSummaryRows.Count == catalog.AnimationBanks.Count &&
            animationPayloadNameCatalogRows == catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count) &&
            animationPayloadNameCatalogRows > 0 &&
            animationPayloadNameTaggedRows == animationPayloadNameCatalogRows &&
            animationPayloadNameMissingRows == 0 &&
            animationPayloadNameSummaryRows.All(row =>
                ReadInt(row, "catalog_rows") == ReadInt(row, "tagged_rows") &&
                ReadInt(row, "missing_rows") == 0);
        var animationPayloadAnchorSummaryRows = ReadTsvRows(
            Path.Combine(reportRoot, "fifa_pc", "animation-bank-catalog-payload-anchor-audit-summary.tsv"));
        var animationPayloadAnchorSummary = animationPayloadAnchorSummaryRows.FirstOrDefault();
        var animationPayloadAnchorBanks = ReadInt(animationPayloadAnchorSummary, "bank_count");
        var animationPayloadAnchorCatalogRows = ReadInt(animationPayloadAnchorSummary, "catalog_rows");
        var animationPayloadAnchorTaggedRows = ReadInt(animationPayloadAnchorSummary, "tagged_rows");
        var animationPayloadAnchorCompleteRows = ReadInt(animationPayloadAnchorSummary, "rows_with_two_anchors");
        var animationPayloadAnchorCount = ReadInt(animationPayloadAnchorSummary, "anchor_count");
        var animationPayloadAnchorIncreasingBanks = ReadInt(animationPayloadAnchorSummary, "banks_strictly_increasing");
        var animationPayloadAnchorCompleteBanks = ReadInt(animationPayloadAnchorSummary, "banks_with_complete_pairs");
        var animationPayloadAnchorVerified =
            animationPayloadAnchorSummary is not null &&
            string.Equals(Get(animationPayloadAnchorSummary, "status"), "verified-structural", StringComparison.Ordinal) &&
            string.Equals(Get(animationPayloadAnchorSummary, "original_semantics"), "not-claimed", StringComparison.Ordinal) &&
            animationPayloadAnchorBanks == catalog.AnimationBanks.Count &&
            animationPayloadAnchorCatalogRows == catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count) &&
            animationPayloadAnchorTaggedRows == animationPayloadAnchorCatalogRows &&
            animationPayloadAnchorCompleteRows == animationPayloadAnchorCatalogRows &&
            animationPayloadAnchorCount == animationPayloadAnchorCatalogRows * 2 &&
            animationPayloadAnchorIncreasingBanks == animationPayloadAnchorBanks &&
            animationPayloadAnchorCompleteBanks == animationPayloadAnchorBanks;
        var animationTailRangeSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "animation-bank-catalog-tail-range-audit-summary.tsv");
        var animationTailRangeSummary = ReadKeyValueTsv(animationTailRangeSummaryPath, "field\tvalue");
        var animationTailRangeVerified =
            HasHeader(animationTailRangeSummaryPath, "field\tvalue") &&
            string.Equals(Get(animationTailRangeSummary, "status"), AnimationBankCatalogTailRangeAudit.Status, StringComparison.Ordinal) &&
            string.Equals(Get(animationTailRangeSummary, "candidate_record_binding"), AnimationBankCatalogTailRangeAudit.CandidateRecordBinding, StringComparison.Ordinal) &&
            string.Equals(Get(animationTailRangeSummary, "original_semantics"), AnimationBankCatalogTailRangeAudit.OriginalSemantics, StringComparison.Ordinal) &&
            ReadInt(animationTailRangeSummary, "catalog_banks") == catalog.AnimationBanks.Count &&
            ReadInt(animationTailRangeSummary, "validated_banks") == catalog.AnimationBanks.Count &&
            ReadInt(animationTailRangeSummary, "catalog_rows") == catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count) &&
            ReadInt(animationTailRangeSummary, "tail_header_shape_valid_rows") == catalog.AnimationBanks.Count &&
            ReadInt(animationTailRangeSummary, "tail_entry_count_match_rows") == catalog.AnimationBanks.Count &&
            ReadInt(animationTailRangeSummary, "tail_range_within_data_rows") == catalog.AnimationBanks.Count &&
            ReadInt(animationTailRangeSummary, "tail_range_aligned_rows") == catalog.AnimationBanks.Count &&
            ReadInt(animationTailRangeSummary, "tail_range_length_match_rows") == catalog.AnimationBanks.Count &&
            ReadInt(animationTailRangeSummary, "tail_range_relocation_count_match_rows") == catalog.AnimationBanks.Count &&
            ReadInt(animationTailRangeSummary, "verified_structural_rows") == catalog.AnimationBanks.Count;
        var animationTailPairSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "animation-bank-tail-pair-relocation-audit-summary.tsv");
        var animationTailPairSummary = ReadKeyValueTsv(animationTailPairSummaryPath, "field\tvalue");
        var animationTailPairVerified =
            HasHeader(animationTailPairSummaryPath, "field\tvalue") &&
            string.Equals(Get(animationTailPairSummary, "status"), AnimationBankTailPairRelocationAudit.Status, StringComparison.Ordinal) &&
            string.Equals(Get(animationTailPairSummary, "candidate_record_binding"), AnimationBankTailPairRelocationAudit.CandidateRecordBinding, StringComparison.Ordinal) &&
            string.Equals(Get(animationTailPairSummary, "original_semantics"), AnimationBankTailPairRelocationAudit.OriginalSemantics, StringComparison.Ordinal) &&
            ReadInt(animationTailPairSummary, "catalog_banks") == catalog.AnimationBanks.Count &&
            ReadInt(animationTailPairSummary, "validated_banks") == catalog.AnimationBanks.Count &&
            ReadInt(animationTailPairSummary, "catalog_rows") == catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count) &&
            ReadInt(animationTailPairSummary, "pair_ordinal_rows") == catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count) &&
            ReadInt(animationTailPairSummary, "pair_rows_with_two_relocations") == catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count) &&
            ReadInt(animationTailPairSummary, "pair_rows_with_expected_offsets") == catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count) &&
            ReadInt(animationTailPairSummary, "rows_with_unexpected_relocation_multiplicity") == 0 &&
            ReadInt(animationTailPairSummary, "first_raw_words_within_data") == catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count) &&
            ReadInt(animationTailPairSummary, "second_raw_words_within_data") == catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count) &&
            ReadInt(animationTailPairSummary, "first_raw_words_at_known_symbols") == 0 &&
            ReadInt(animationTailPairSummary, "second_raw_words_at_known_symbols") == 0 &&
            ReadInt(animationTailPairSummary, "distinct_raw_word_pairs") == catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count) &&
            ReadInt(animationTailPairSummary, "banks_with_complete_pair_layout") == catalog.AnimationBanks.Count;
        var animationTailTargetChainSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "animation-bank-tail-target-chain-audit-summary.tsv");
        var animationTailTargetChainSummary = ReadKeyValueTsv(animationTailTargetChainSummaryPath, "field\tvalue");
        var animationTailTargetChainVerified =
            HasHeader(animationTailTargetChainSummaryPath, "field\tvalue") &&
            string.Equals(Get(animationTailTargetChainSummary, "status"), AnimationBankTailPairTargetChainAudit.Status, StringComparison.Ordinal) &&
            string.Equals(Get(animationTailTargetChainSummary, "candidate_record_binding"), AnimationBankTailPairTargetChainAudit.CandidateRecordBinding, StringComparison.Ordinal) &&
            string.Equals(Get(animationTailTargetChainSummary, "original_semantics"), AnimationBankTailPairTargetChainAudit.OriginalSemantics, StringComparison.Ordinal) &&
            ReadInt(animationTailTargetChainSummary, "catalog_banks") == catalog.AnimationBanks.Count &&
            ReadInt(animationTailTargetChainSummary, "validated_banks") == catalog.AnimationBanks.Count &&
            ReadInt(animationTailTargetChainSummary, "catalog_rows") == catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count) &&
            ReadInt(animationTailTargetChainSummary, "pair_rows") == catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count) &&
            ReadInt(animationTailTargetChainSummary, "first_target_rows_with_one_relocation") == 0 &&
            ReadInt(animationTailTargetChainSummary, "second_target_rows_with_one_relocation") == 0 &&
            ReadInt(animationTailTargetChainSummary, "first_second_level_words_within_data") == 0 &&
            ReadInt(animationTailTargetChainSummary, "second_second_level_words_within_data") == 0 &&
            ReadInt(animationTailTargetChainSummary, "first_second_level_words_at_known_symbols") == 0 &&
            ReadInt(animationTailTargetChainSummary, "second_second_level_words_at_known_symbols") == 0 &&
            ReadInt(animationTailTargetChainSummary, "rows_with_complete_target_chain") == catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count) &&
            ReadInt(animationTailTargetChainSummary, "banks_with_complete_target_chain") == catalog.AnimationBanks.Count;
        var animationTimestampAuditReportPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "animation-bank-catalog-timestamp-audit.tsv");
        var animationTimestampAuditSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "animation-bank-catalog-timestamp-audit-summary.tsv");
        var animationTimestampAuditRows = ReadTsvRows(animationTimestampAuditReportPath);
        var animationTimestampAuditSummary = ReadTsvRows(animationTimestampAuditSummaryPath).FirstOrDefault();
        var animationTimestampAuditVerified =
            HasHeader(animationTimestampAuditReportPath, AnimationBankCatalogTimestampAudit.ReportHeader) &&
            HasHeader(animationTimestampAuditSummaryPath, AnimationBankCatalogTimestampAudit.SummaryHeader) &&
            animationTimestampAuditSummary is not null &&
            string.Equals(Get(animationTimestampAuditSummary, "status"), "verified-structural", StringComparison.Ordinal) &&
            string.Equals(Get(animationTimestampAuditSummary, "direct_timing_binding"), "not-proven", StringComparison.Ordinal) &&
            string.Equals(Get(animationTimestampAuditSummary, "original_semantics"), "not-claimed", StringComparison.Ordinal) &&
            ReadInt(animationTimestampAuditSummary, "bank_count") == catalog.AnimationBanks.Count &&
            ReadInt(animationTimestampAuditSummary, "catalog_rows") == catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count) &&
            ReadInt(animationTimestampAuditSummary, "display_date_parseable_rows") == ReadInt(animationTimestampAuditSummary, "catalog_rows") &&
            ReadInt(animationTimestampAuditSummary, "exact_timestamp_date_rows") == ReadInt(animationTimestampAuditSummary, "catalog_rows") &&
            ReadInt(animationTimestampAuditSummary, "timestamp_date_mismatch_rows") == 0 &&
            ReadInt(animationTimestampAuditSummary, "invalid_timestamp_rows") == 0 &&
            animationTimestampAuditRows.Count == catalog.AnimationBanks.Count &&
            animationTimestampAuditRows.All(row =>
                string.Equals(Get(row, "status"), "verified-structural", StringComparison.Ordinal) &&
                string.Equals(Get(row, "evidence_class"), "catalog-source-line-date-correspondence", StringComparison.Ordinal));

        var namedAudio = catalog.AudioEventCandidates.Count;
        var namedAudioWithPayloadLayout = catalog.AudioEventCandidates.Count(
            candidate => candidate.PayloadChannelLayoutCandidate is not null);
        var namedEventRuntimeSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-named-event-runtime-summary.tsv");
        var namedEventRuntimeSummary = ReadKeyValueTsv(
            namedEventRuntimeSummaryPath,
            "field\tvalue");
        var namedEventRuntimeVerified =
            HasHeader(namedEventRuntimeSummaryPath, "field\tvalue") &&
            string.Equals(
                Get(namedEventRuntimeSummary, "status"),
                "verified-runtime-named-payload",
                StringComparison.Ordinal) &&
            string.Equals(
                Get(namedEventRuntimeSummary, "provenance"),
                AudioNamedEventRuntimeAudit.Provenance,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(namedEventRuntimeSummary, "original_semantics"),
                AudioNamedEventRuntimeAudit.OriginalSemantics,
                StringComparison.Ordinal) &&
            ReadInt(namedEventRuntimeSummary, "candidate_count") == namedAudio &&
            ReadInt(namedEventRuntimeSummary, "candidate_metadata_validated") == namedAudio &&
            ReadInt(namedEventRuntimeSummary, "runtime_prepared") == namedAudio &&
            ReadInt(namedEventRuntimeSummary, "wave_headers_valid") == namedAudio;
        var audioParallelRangeSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-parallel-range-summary.tsv");
        var audioParallelRangeSummary = ReadKeyValueTsv(
            audioParallelRangeSummaryPath,
            AudioEventParallelRangeAuditSummary.Header);
        var audioParallelRangeVerified =
            HasHeader(audioParallelRangeSummaryPath, AudioEventParallelRangeAuditSummary.Header) &&
            string.Equals(
                Get(audioParallelRangeSummary, "status"),
                AudioEventParallelRangeAudit.Status,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(audioParallelRangeSummary, "provenance"),
                AudioEventParallelRangeAudit.Provenance,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(audioParallelRangeSummary, "direct_selection_binding"),
                AudioEventParallelRangeAudit.DirectSelectionBinding,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(audioParallelRangeSummary, "original_semantics"),
                AudioEventParallelRangeAudit.OriginalSemantics,
                StringComparison.Ordinal) &&
            ReadInt(audioParallelRangeSummary, "candidate_count") == namedAudio &&
            ReadInt(audioParallelRangeSummary, "event_string_anchor_valid") == namedAudio &&
            ReadInt(audioParallelRangeSummary, "header_range_valid") == namedAudio &&
            ReadInt(audioParallelRangeSummary, "data_range_valid") == namedAudio &&
            ReadInt(audioParallelRangeSummary, "parallel_row_index_valid") == namedAudio &&
            ReadInt(audioParallelRangeSummary, "data_schl_prefix") == namedAudio &&
            ReadInt(audioParallelRangeSummary, "unique_event_names") == namedAudio &&
            ReadInt(audioParallelRangeSummary, "unique_event_string_offsets") == namedAudio &&
            ReadInt(audioParallelRangeSummary, "unique_header_rows") == namedAudio &&
            ReadInt(audioParallelRangeSummary, "verified_rows") == namedAudio &&
            ReadInt(audioParallelRangeSummary, "header_table_rows") > 0 &&
            ReadInt(audioParallelRangeSummary, "data_table_rows") > 0 &&
            ReadInt(audioParallelRangeSummary, "header_table_rows") ==
                ReadInt(audioParallelRangeSummary, "data_table_rows");
        var audioXbeHashSearchSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-xbe-hash-search-summary.tsv");
        var audioXbeHashSearchSummary = ReadKeyValueTsv(
            audioXbeHashSearchSummaryPath,
            AudioEventXbeHashSearchSummary.Header);
        var audioXbeHashSearchVerified =
            HasHeader(audioXbeHashSearchSummaryPath, AudioEventXbeHashSearchSummary.Header) &&
            string.Equals(
                Get(audioXbeHashSearchSummary, "status"),
                AudioEventXbeHashSearchAudit.Status,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(audioXbeHashSearchSummary, "provenance"),
                AudioEventXbeHashSearchAudit.Provenance,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(audioXbeHashSearchSummary, "direct_selection_binding"),
                AudioEventXbeHashSearchAudit.DirectSelectionBinding,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(audioXbeHashSearchSummary, "original_semantics"),
                AudioEventXbeHashSearchAudit.OriginalSemantics,
                StringComparison.Ordinal) &&
            ReadInt(audioXbeHashSearchSummary, "candidate_count") == namedAudio &&
            ReadInt(audioXbeHashSearchSummary, "unique_event_names") == namedAudio &&
            ReadInt(audioXbeHashSearchSummary, "unique_hash_words") == namedAudio &&
            ReadInt(audioXbeHashSearchSummary, "xbe_bytes") > 0;
        var fifast2CsiTunaSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "fifast2-csi-tuna-hash-crosscheck-summary.tsv");
        var fifast2CsiTunaSummary = ReadKeyValueTsv(
            fifast2CsiTunaSummaryPath,
            Fifast2CsiTunaAuditSummary.Header);
        var fifast2CsiTunaVerified =
            HasHeader(fifast2CsiTunaSummaryPath, Fifast2CsiTunaAuditSummary.Header) &&
            string.Equals(
                Get(fifast2CsiTunaSummary, "status"),
                Fifast2CsiTunaAudit.Status,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(fifast2CsiTunaSummary, "provenance"),
                Fifast2CsiTunaAudit.Provenance,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(fifast2CsiTunaSummary, "direct_selection_binding"),
                Fifast2CsiTunaAudit.DirectSelectionBinding,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(fifast2CsiTunaSummary, "original_semantics"),
                Fifast2CsiTunaAudit.OriginalSemantics,
                StringComparison.Ordinal) &&
            ReadInt(fifast2CsiTunaSummary, "tuna_row_count") == 106 &&
            ReadInt(fifast2CsiTunaSummary, "tuna_hash_match_count") == 106 &&
            ReadInt(fifast2CsiTunaSummary, "csi_record_count") == 100 &&
            ReadInt(fifast2CsiTunaSummary, "csi_valid_record_count") == 100 &&
            ReadInt(fifast2CsiTunaSummary, "csi_name_overlap_count") == 89 &&
            ReadInt(fifast2CsiTunaSummary, "csi_rows_with_tuna_hash_match") == 89 &&
            ReadInt(fifast2CsiTunaSummary, "csi_opaque_hash_match_count") == 0;
        var fifast2CsiOpaqueRangeSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "fifast2-csi-opaque-range-audit-summary.tsv");
        var fifast2CsiOpaqueRangeSummary = ReadKeyValueTsv(
            fifast2CsiOpaqueRangeSummaryPath,
            Fifast2CsiOpaqueRangeAuditSummary.Header);
        var fifast2CsiOpaqueRangeVerified =
            HasHeader(fifast2CsiOpaqueRangeSummaryPath, Fifast2CsiOpaqueRangeAuditSummary.Header) &&
            string.Equals(
                Get(fifast2CsiOpaqueRangeSummary, "status"),
                Fifast2CsiOpaqueRangeAudit.Status,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(fifast2CsiOpaqueRangeSummary, "provenance"),
                Fifast2CsiOpaqueRangeAudit.Provenance,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(fifast2CsiOpaqueRangeSummary, "opaque_word_semantics"),
                Fifast2CsiOpaqueRangeAudit.OpaqueWordSemantics,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(fifast2CsiOpaqueRangeSummary, "direct_selection_binding"),
                Fifast2CsiOpaqueRangeAudit.DirectSelectionBinding,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(fifast2CsiOpaqueRangeSummary, "original_semantics"),
                Fifast2CsiOpaqueRangeAudit.OriginalSemantics,
                StringComparison.Ordinal) &&
            ReadInt(fifast2CsiOpaqueRangeSummary, "csi_record_count") == 100 &&
            ReadInt(fifast2CsiOpaqueRangeSummary, "csi_valid_record_count") == 100 &&
            ReadInt(fifast2CsiOpaqueRangeSummary, "pcpahdr_row_count") == 283 &&
            ReadInt(fifast2CsiOpaqueRangeSummary, "pcpadat_row_count") == 283 &&
            ReadInt(fifast2CsiOpaqueRangeSummary, "csi_opaque_within_pcpahdr_count") == 100 &&
            ReadInt(fifast2CsiOpaqueRangeSummary, "csi_opaque_pcpahdr_exact_start_count") == 0 &&
            ReadInt(fifast2CsiOpaqueRangeSummary, "csi_opaque_pcpahdr_exact_end_count") == 0 &&
            ReadInt(fifast2CsiOpaqueRangeSummary, "csi_opaque_within_pcpadat_count") == 100 &&
            ReadInt(fifast2CsiOpaqueRangeSummary, "csi_opaque_pcpadat_exact_start_count") == 0 &&
            ReadInt(fifast2CsiOpaqueRangeSummary, "csi_opaque_pcpadat_exact_end_count") == 0;
        var fifast2CsiOpaqueFieldSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "fifast2-csi-opaque-field-audit-summary.tsv");
        var fifast2CsiOpaqueFieldSummary = ReadKeyValueTsv(
            fifast2CsiOpaqueFieldSummaryPath,
            Fifast2CsiOpaqueFieldAuditSummary.Header);
        var fifast2CsiOpaqueFieldVerified =
            HasHeader(fifast2CsiOpaqueFieldSummaryPath, Fifast2CsiOpaqueFieldAuditSummary.Header) &&
            string.Equals(
                Get(fifast2CsiOpaqueFieldSummary, "status"),
                Fifast2CsiOpaqueFieldAudit.Status,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(fifast2CsiOpaqueFieldSummary, "provenance"),
                Fifast2CsiOpaqueFieldAudit.Provenance,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(fifast2CsiOpaqueFieldSummary, "opaque_word_semantics"),
                Fifast2CsiOpaqueFieldAudit.OpaqueWordSemantics,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(fifast2CsiOpaqueFieldSummary, "direct_selection_binding"),
                Fifast2CsiOpaqueFieldAudit.DirectSelectionBinding,
                StringComparison.Ordinal) &&
            string.Equals(
                Get(fifast2CsiOpaqueFieldSummary, "original_semantics"),
                Fifast2CsiOpaqueFieldAudit.OriginalSemantics,
                StringComparison.Ordinal) &&
            ReadInt(fifast2CsiOpaqueFieldSummary, "csi_record_count") == 100 &&
            ReadInt(fifast2CsiOpaqueFieldSummary, "csi_valid_record_count") == 100 &&
            ReadInt(fifast2CsiOpaqueFieldSummary, "pcpahdr_row_count") == 283 &&
            string.Equals(Get(fifast2CsiOpaqueFieldSummary, "pcpahdr_partition_coverage"), "True", StringComparison.Ordinal) &&
            ReadInt(fifast2CsiOpaqueFieldSummary, "pcpadat_row_count") == 283 &&
            string.Equals(Get(fifast2CsiOpaqueFieldSummary, "pcpadat_partition_coverage"), "True", StringComparison.Ordinal) &&
            ReadInt(fifast2CsiOpaqueFieldSummary, "pcpahdr_any_column_match_count") == 1 &&
            ReadInt(fifast2CsiOpaqueFieldSummary, "pcpahdr_column1_match_count") == 0 &&
            ReadInt(fifast2CsiOpaqueFieldSummary, "pcpahdr_column2_match_count") == 1 &&
            ReadInt(fifast2CsiOpaqueFieldSummary, "pcpahdr_column3_match_count") == 0 &&
            ReadInt(fifast2CsiOpaqueFieldSummary, "pcpahdr_row_ordinal_match_count") == 3 &&
            ReadInt(fifast2CsiOpaqueFieldSummary, "pcpadat_any_column_match_count") == 0 &&
            ReadInt(fifast2CsiOpaqueFieldSummary, "pcpadat_column1_match_count") == 0 &&
            ReadInt(fifast2CsiOpaqueFieldSummary, "pcpadat_column2_match_count") == 0 &&
            ReadInt(fifast2CsiOpaqueFieldSummary, "pcpadat_column3_match_count") == 0 &&
            ReadInt(fifast2CsiOpaqueFieldSummary, "pcpadat_row_ordinal_match_count") == 3;
        var audioEventIndexReportPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-index-evidence.tsv");
        var audioEventIndexReport = ReadKeyValueTsv(audioEventIndexReportPath);
        var audioEventIndexReportVerified =
            HasHeader(audioEventIndexReportPath, AudioEventIndexEvidenceReport.Header) &&
            string.Equals(Get(audioEventIndexReport, "status"), "no-direct-selection-binding", StringComparison.Ordinal) &&
            string.Equals(Get(audioEventIndexReport, "direct_selection_binding"), "not-proven", StringComparison.Ordinal) &&
            string.Equals(Get(audioEventIndexReport, "original_semantics"), "not-claimed", StringComparison.Ordinal);
        var audioPayloadWaveCorrelationReportPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-payload-wave-correlation-summary.tsv");
        var audioPayloadWaveCorrelationSummary = ReadKeyValueTsv(
            audioPayloadWaveCorrelationReportPath,
            "field\tvalue");
        var audioPayloadWaveCorrelationVerified =
            HasHeader(audioPayloadWaveCorrelationReportPath, "field\tvalue") &&
            string.Equals(Get(audioPayloadWaveCorrelationSummary, "status"), "verified-structural", StringComparison.Ordinal) &&
            string.Equals(Get(audioPayloadWaveCorrelationSummary, "direct_selection_binding"), "not-proven", StringComparison.Ordinal) &&
            string.Equals(Get(audioPayloadWaveCorrelationSummary, "original_semantics"), "not-claimed", StringComparison.Ordinal) &&
            ReadInt(audioPayloadWaveCorrelationSummary, "event_candidates") == catalog.AudioEventCandidates.Count &&
            ReadInt(audioPayloadWaveCorrelationSummary, "bank_candidates") == catalog.AudioBanks.Count &&
            ReadInt(audioPayloadWaveCorrelationSummary, "event_decoded") > 0 &&
            ReadInt(audioPayloadWaveCorrelationSummary, "bank_decoded") > 0;
        var audioPayloadWaveExhaustiveSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-payload-wave-correlation-exhaustive-summary.tsv");
        var audioPayloadWaveExhaustiveSummary = ReadKeyValueTsv(
            audioPayloadWaveExhaustiveSummaryPath,
            "field\tvalue");
        var audioPayloadWaveExhaustiveVerified =
            HasHeader(audioPayloadWaveExhaustiveSummaryPath, "field\tvalue") &&
            string.Equals(Get(audioPayloadWaveExhaustiveSummary, "status"), "verified-structural", StringComparison.Ordinal) &&
            string.Equals(Get(audioPayloadWaveExhaustiveSummary, "direct_selection_binding"), "not-proven", StringComparison.Ordinal) &&
            string.Equals(Get(audioPayloadWaveExhaustiveSummary, "original_semantics"), "not-claimed", StringComparison.Ordinal) &&
            ReadInt(audioPayloadWaveExhaustiveSummary, "event_candidates") == catalog.AudioEventCandidates.Count &&
            ReadInt(audioPayloadWaveExhaustiveSummary, "bank_count") == catalog.AudioBanks.Count &&
            ReadInt(audioPayloadWaveExhaustiveSummary, "event_decoded") > 0 &&
            ReadInt(audioPayloadWaveExhaustiveSummary, "bank_sound_candidates") > 0 &&
            ReadInt(audioPayloadWaveExhaustiveSummary, "bank_sound_decoded") > 0;
        var audioPayloadWaveAllSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-payload-wave-correlation-all-summary.tsv");
        var audioPayloadWaveAllSummary = ReadKeyValueTsv(
            audioPayloadWaveAllSummaryPath,
            "field\tvalue");
        var audioPayloadWaveAllVerified =
            HasHeader(audioPayloadWaveAllSummaryPath, "field\tvalue") &&
            string.Equals(Get(audioPayloadWaveAllSummary, "status"), "verified-structural", StringComparison.Ordinal) &&
            string.Equals(Get(audioPayloadWaveAllSummary, "direct_selection_binding"), "not-proven", StringComparison.Ordinal) &&
            string.Equals(Get(audioPayloadWaveAllSummary, "original_semantics"), "not-claimed", StringComparison.Ordinal) &&
            string.Equals(Get(audioPayloadWaveAllSummary, "bounded_policy"), "first_100000_sound_candidates_per_bank", StringComparison.Ordinal) &&
            ReadInt(audioPayloadWaveAllSummary, "event_candidates") == catalog.AudioEventCandidates.Count &&
            ReadInt(audioPayloadWaveAllSummary, "bank_count") == catalog.AudioBanks.Count &&
            ReadInt(audioPayloadWaveAllSummary, "event_decoded") == catalog.AudioEventCandidates.Count &&
            ReadInt(audioPayloadWaveAllSummary, "bank_sound_candidates") > 0 &&
            ReadInt(audioPayloadWaveAllSummary, "bank_sound_decoded") > 0 &&
            ReadInt(audioPayloadWaveAllSummary, "exact_pcm_hash_pairs") == 0 &&
            ReadInt(audioPayloadWaveAllSummary, "event_hashes_with_match") == 0;
        var audioNamedPayloadIdentitySummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-named-payload-identity-control-summary.tsv");
        var audioNamedPayloadIdentityEvidence =
            ReadAudioNamedPayloadIdentityControlEvidence(
                audioNamedPayloadIdentitySummaryPath,
                ReadTsvRows(audioNamedPayloadIdentitySummaryPath).FirstOrDefault());
        var audioPayloadPolicyControlSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-payload-policy-control-summary.tsv");
        var audioPayloadPolicyControlSummaryRows = ReadTsvRows(audioPayloadPolicyControlSummaryPath);
        var audioPayloadPolicyControlSummary = audioPayloadPolicyControlSummaryRows.FirstOrDefault();
        var audioPayloadPolicyControlEvidence = ReadAudioPayloadPolicyControlEvidence(
            audioPayloadPolicyControlSummaryPath,
            audioPayloadPolicyControlSummary);
        var audioBankDecoderSelectionSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-bank-decoder-selection-control-summary.tsv");
        var audioBankDecoderSelectionEvidence = ReadAudioBankDecoderSelectionControlEvidence(
            audioBankDecoderSelectionSummaryPath,
            ReadTsvRows(audioBankDecoderSelectionSummaryPath).FirstOrDefault());
        var audioParallelRuntimeJoinSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-parallel-runtime-join-control-summary.tsv");
        var audioParallelRuntimeJoinSummary = ReadAudioParallelRuntimeJoinControlEvidence(
            audioParallelRuntimeJoinSummaryPath,
            ReadTsvRows(audioParallelRuntimeJoinSummaryPath).FirstOrDefault());
        var audioEventEvtPcpRangeSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-evt-pcp-range-discrimination-control-summary.tsv");
        var audioEventEvtPcpRangeEvidence = ReadAudioEventEvtPcpRangeDiscriminationEvidence(
            audioEventEvtPcpRangeSummaryPath,
            ReadTsvRows(audioEventEvtPcpRangeSummaryPath).FirstOrDefault());
        var audioEventAudioAssetHashUniverseSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-audio-asset-hash-universe-summary.tsv");
        var audioEventAudioAssetHashUniverseEvidence = ReadAudioEventAudioAssetHashUniverseEvidence(
            audioEventAudioAssetHashUniverseSummaryPath,
            ReadKeyValueTsv(audioEventAudioAssetHashUniverseSummaryPath, "field\tvalue"));
        var audioEventDataAssetHashUniverseSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-data-asset-hash-universe-summary.tsv");
        var audioEventDataAssetHashUniverseEvidence = ReadAudioEventDataAssetHashUniverseEvidence(
            audioEventDataAssetHashUniverseSummaryPath,
            ReadKeyValueTsv(audioEventDataAssetHashUniverseSummaryPath, "field\tvalue"));
        var audioEventDataAssetHashUniverseControlSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-data-asset-hash-universe-control-summary.tsv");
        var audioEventDataAssetHashUniverseControlEvidence = ReadAudioEventDataAssetHashUniverseControlEvidence(
            audioEventDataAssetHashUniverseControlSummaryPath,
            ReadKeyValueTsv(audioEventDataAssetHashUniverseControlSummaryPath, "field\tvalue"));
        var audioEventNameTextSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-name-asset-text-search-summary.tsv");
        var audioEventNameTextEvidence = ReadAudioEventNameTextSearchEvidence(
            audioEventNameTextSummaryPath,
            ReadKeyValueTsv(audioEventNameTextSummaryPath, "field\tvalue"));
        var audioEventNameTextControlSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-name-asset-text-search-control-summary.tsv");
        var audioEventNameTextControlEvidence = ReadAudioEventNameTextSearchControlEvidence(
            audioEventNameTextControlSummaryPath,
            ReadTsvRows(audioEventNameTextControlSummaryPath).FirstOrDefault());
        var authoredAudioCandidateRouteSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-authored-candidate-route-control-summary.tsv");
        var authoredAudioCandidateRouteEvidence = ReadAuthoredAudioCandidateRouteControlEvidence(
            authoredAudioCandidateRouteSummaryPath,
            ReadTsvRows(authoredAudioCandidateRouteSummaryPath).FirstOrDefault());
        var authoredNamedPayloadIdentitySummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-authored-named-payload-identity-control-summary.tsv");
        var authoredNamedPayloadIdentityEvidence = ReadAuthoredNamedPayloadIdentityControlEvidence(
            authoredNamedPayloadIdentitySummaryPath,
            ReadTsvRows(authoredNamedPayloadIdentitySummaryPath).FirstOrDefault());
        var audioEventAbkRawIdentitySummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-abk-raw-identity-summary.tsv");
        var audioEventAbkRawIdentityControlSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-abk-raw-identity-control-summary.tsv");
        var audioEventAbkRawIdentityEvidence = ReadAudioEventAbkRawIdentityEvidence(
            audioEventAbkRawIdentitySummaryPath,
            audioEventAbkRawIdentityControlSummaryPath,
            ReadKeyValueTsv(audioEventAbkRawIdentitySummaryPath, "field\tvalue"),
            ReadKeyValueTsv(audioEventAbkRawIdentityControlSummaryPath, "field\tvalue"));
        var audioSelectionNegativeSynthesisSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-selection-negative-synthesis-summary.tsv");
        var audioSelectionNegativeSynthesisSummary = ReadKeyValueTsv(
            audioSelectionNegativeSynthesisSummaryPath,
            "field\tvalue");
        var audioSelectionNegativeSynthesisVerified =
            HasHeader(audioSelectionNegativeSynthesisSummaryPath, "field\tvalue") &&
            string.Equals(
                Get(audioSelectionNegativeSynthesisSummary, "status"),
                "verified-independent-audio-selection-negative-synthesis",
                StringComparison.Ordinal) &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "source_count") == 5 &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "valid_source_count") == 5 &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "index_candidate_count") == namedAudio &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "index_abk_occurrences") == 0 &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "index_header_occurrences") == 0 &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "index_data_occurrences") == 0 &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "xbe_candidate_count") == namedAudio &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "xbe_aligned_occurrences") == 0 &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "xbe_unaligned_occurrences") == 0 &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "payload_event_candidates") == namedAudio &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "payload_event_decoded") == namedAudio &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "payload_exact_pcm_hash_pairs") == 0 &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "payload_event_hashes_with_match") == 0 &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "parallel_joined_rows") == namedAudio &&
            ReadInt(audioSelectionNegativeSynthesisSummary, "authored_route_rows") == CleanRoomAudioCuePolicy.Routes.Count &&
            string.Equals(
                Get(audioSelectionNegativeSynthesisSummary, "direct_selection_binding"),
                "not-proven",
                StringComparison.Ordinal) &&
            string.Equals(
                Get(audioSelectionNegativeSynthesisSummary, "original_semantics"),
                "not-claimed",
                StringComparison.Ordinal);
        var fifast2CsiEvtNameAmbiguitySummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "fifast2-csi-evt-name-ambiguity-control-summary.tsv");
        var fifast2CsiEvtNameAmbiguityEvidence =
            ReadFifast2CsiEvtNameAmbiguityControlEvidence(
                fifast2CsiEvtNameAmbiguitySummaryPath,
                ReadTsvRows(fifast2CsiEvtNameAmbiguitySummaryPath).FirstOrDefault());
        var audioEventSampleGlobalAssignmentSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-sample-global-assignment-control-summary.tsv");
        var audioEventSampleGlobalAssignmentEvidence =
            ReadAudioEventSampleGlobalAssignmentControlEvidence(
                audioEventSampleGlobalAssignmentSummaryPath,
                ReadTsvRows(audioEventSampleGlobalAssignmentSummaryPath).FirstOrDefault());
        var audioEventAssetPathSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-asset-path-index-control-summary.tsv");
        var audioEventAssetPathSummaryRows = ReadTsvRows(audioEventAssetPathSummaryPath);
        var audioEventAssetPathSummary = audioEventAssetPathSummaryRows.FirstOrDefault();
        const string audioEventAssetPathSummaryHeader =
            "event_path\tcandidate_name_count\tcandidate_pairs\txbe_literal_rows\txbe_literal_occurrences\txbe_literal_xref_count\tmatched_path_rows\tdistinct_matched_paths\tstatus\tprovenance\tdirect_selection_binding\toriginal_semantics";
        var audioEventAssetPathControlVerified =
            HasHeader(audioEventAssetPathSummaryPath, audioEventAssetPathSummaryHeader) &&
            string.Equals(
                Get(audioEventAssetPathSummary, "event_path"),
                "data/audio/eventspa.evt",
                StringComparison.Ordinal) &&
            string.Equals(
                Get(audioEventAssetPathSummary, "status"),
                "verified-independent-audio-event-asset-path-index-control",
                StringComparison.Ordinal) &&
            ReadInt(audioEventAssetPathSummary, "candidate_name_count") == catalog.AudioEventCandidates.Count &&
            ReadInt(audioEventAssetPathSummary, "candidate_pairs") > 0 &&
            ReadInt(audioEventAssetPathSummary, "xbe_literal_rows") == 4 &&
            ReadInt(audioEventAssetPathSummary, "xbe_literal_occurrences") == 9 &&
            ReadInt(audioEventAssetPathSummary, "xbe_literal_xref_count") == 0 &&
            ReadInt(audioEventAssetPathSummary, "matched_path_rows") == 4 &&
            ReadInt(audioEventAssetPathSummary, "distinct_matched_paths") == 1 &&
            string.Equals(
                Get(audioEventAssetPathSummary, "direct_selection_binding"),
                "not-proven",
                StringComparison.Ordinal) &&
            string.Equals(
                Get(audioEventAssetPathSummary, "original_semantics"),
                "not-claimed",
                StringComparison.Ordinal);
        var audioEventStaticContextSummaryPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "audio-event-static-context-global-assignment-control-summary.tsv");
        var audioEventStaticContextEvidence =
            ReadAudioEventStaticContextGlobalAssignmentControlEvidence(
                audioEventStaticContextSummaryPath);
        var audioCoverageReportPath = Path.Combine(
            reportRoot,
            "fifa_pc",
            "gameplay-audio-coverage.tsv");
        var audioCoverageRows = ReadTsvRows(audioCoverageReportPath);
        var audioCoverageSummary = audioCoverageRows.FirstOrDefault(row =>
            string.Equals(Get(row, "kind"), "summary", StringComparison.Ordinal));
        var audioCoverageVerified =
            audioCoverageSummary is not null &&
            string.Equals(Get(audioCoverageSummary, "status"), "verified-authored", StringComparison.Ordinal) &&
            string.Equals(Get(audioCoverageSummary, "missing_policy_actions"), string.Empty, StringComparison.Ordinal) &&
            string.Equals(Get(audioCoverageSummary, "original_semantics"), "not-claimed", StringComparison.Ordinal);
        var cleanRoomAudioGatePassed =
            authoredAudioRuntimeVerified &&
            authoredAudioAssetRuntimeVerified &&
            audioCoverageVerified &&
            authoredIntegratedCoverageVerified;
        var authoredScenarioCount = Math.Min(
            Math.Min(
                ReadInt(gameplayCoverageSummary, "scenario_count"),
                ReadInt(gameplayInvariantSummary, "scenario_count")),
            Math.Min(
                ReadInt(authoredIntegratedCoverageSummary, "scenario_count"),
                ReadInt(audioCoverageSummary, "scenario_count")));
        var authoredRuntimeVerified =
            gameplayCoverageVerified &&
            gameplayInvariantVerified &&
            authoredIntegratedCoverageVerified &&
            audioCoverageVerified;
        var authoredAudioRouteCount = CleanRoomAudioCuePolicy.Routes.Count;
        var authoredAudioUniqueCueCount = CleanRoomAudioCuePolicy.Routes
            .Select(route => route.CandidateName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var authoredAudioRoutesAvailable = CleanRoomAudioCuePolicy.Routes.Count(route =>
            catalog.AudioEventCandidates.Any(candidate =>
                string.Equals(candidate.Candidate.EventName, route.CandidateName, StringComparison.OrdinalIgnoreCase) &&
                candidate.Candidate.ParallelDataRow is { SecondColumn: >= 8 } &&
                (candidate.Info is { Channels: > 0 } || candidate.PayloadChannelLayoutCandidate is not null)));
        var abkValidated = catalog.AudioBanks.Sum(bank => bank.BnkXboxImaDataValidatedCount);
        var abkRejected = catalog.AudioBanks.Sum(bank => bank.BnkXboxImaDataRejectedCount);
        var abkAttempted = abkValidated + abkRejected;

        var isoGate = length == KnownIsoLength &&
                      string.Equals(sha256, KnownIsoSha256, StringComparison.OrdinalIgnoreCase) &&
                      (attributes & FileAttributes.ReadOnly) == 0;

        var rows = new List<ProjectReadinessRow>
        {
            new(
                "iso-integrity",
                isoGate ? "verified" : "mismatch",
                isoGate,
                length,
                KnownIsoLength,
                "read-only FileStream + SHA-256 + file attributes",
                "preserve the same ISO bytes while the remaining gates are implemented",
                $"sha256={sha256}; attributes={attributes}"),
            new(
                "assets",
                catalog.CandidateMaterials.Count > 0 &&
                catalog.CandidateMaterials.All(material => material.DecodedRgbaBytes > 0)
                    ? "candidate-integrated"
                    : "incomplete",
                false,
                catalog.CandidateMaterials.Count,
                catalog.CandidateMaterials.Count,
                "same-trunk model -> nested .ord/.orl structural cross-check -> SHAPENAME -> SHPX -> decoded image -> managed candidate renderer",
                "independent mesh/texture/material binding evidence for the runtime draw path",
                $"textures_decoded={catalog.CandidateMaterials.Count(material => material.DecodedRgbaBytes > 0)}; PAL8={catalog.CandidateMaterials.Count(material => string.Equals(material.PixelFormat, "PAL8", StringComparison.OrdinalIgnoreCase))}; candidate_source_chain_verified={(candidateSourceChainVerified ? catalog.CandidateMaterials.Count : 0)}; candidate_mesh_state_crosscheck_verified={(candidateMeshStateCrosscheckVerified ? catalog.CandidateMaterials.Count : 0)}; runtime_candidate_frames={candidateRuntimeVerified}; runtime_candidate_rejections={candidateRuntimeRejected}; runtime_candidate_distinct_checksums={candidateRuntimeChecksums}; runtime_candidate_home_slot_frames={candidateRuntimeHomeSlots}; runtime_candidate_away_slot_frames={candidateRuntimeAwaySlots}; runtime_candidate_both_slot_frames={candidateRuntimeBothSlots}; candidate_render_influence_rows={candidateRenderInfluenceRows.Length}; candidate_render_influence_verified={candidateRenderInfluenceVerified}; candidate_texture_influences_raster={candidateRenderInfluenceTextureRows}; isolated_candidate_texture_influences_raster={candidateRenderInfluenceIsolatedRows}; candidate_geometry_finite_and_indexed={candidateRenderInfluenceGeometryRows}; candidate_render_influence_semantics=not-claimed"),
            new(
                "rendering",
                exactStatePairs > 0 ? "candidate-only" : "not-proven",
                false,
                exactStatePairs,
                1,
                "eagl-candidate-state-correspondence.tsv exact raw-byte pairs and shared relocation symbols",
                "a verified association from render state to geometry plus independently validated UV/material interpretation",
                $"candidate_materials={catalog.CandidateMaterials.Count}; state_rows={stateCorrespondence.Count}; exact_state_pairs={exactStatePairs}; shared_relocation_pairs={sharedRelocationPairs}; state_geometry_adjacency_rows={stateGeometryAdjacencyVerified}; same_primitive_index_pairs={samePrimitiveIndexPairs}; adjacent_primitive_pairs={adjacentPrimitivePairs}; same_layer_pairs={sameLayerPairs}; state_geometry_profile_rows={stateGeometryProfilesVerified}; exact_command_shape_pairs={exactCommandShapePairs}; unique_state_profile_rows={uniqueStateProfileRows}; distinct_geometry_command_shapes={distinctGeometryCommandShapes}; distinct_state_command_shapes={distinctStateCommandShapes}; state_geometry_order_rows={stateGeometryOrderVerified}; state_geometry_order_nonempty_runs={stateGeometryOrderNonEmpty}; state_geometry_order_partitioned_states={stateGeometryOrderPartitioned}; shpx_geometry_rows={shpxGeometryCorrelation.Count}; shpx_geometry_both_variable={shpxGeometryBothVariable}; shpx_geometry_variable={shpxGeometryVariable}; shpx_variable={shpxVariable}; candidate_render_influence_verified={candidateRenderInfluenceVerified}; candidate_texture_influences_raster={candidateRenderInfluenceTextureRows}; isolated_candidate_texture_influences_raster={candidateRenderInfluenceIsolatedRows}; candidate_render_influence_geometry_rows={candidateRenderInfluenceGeometryRows}; candidate_render_influence_semantics=not-claimed"),
            new(
                "gameplay",
                cleanRoomGameplayGatePassed ? "clean-room-integrated" : "not-present",
                cleanRoomGameplayGatePassed,
                1,
                1,
                "deterministic match simulation with possession, pass, trick, juggle, shot, goal, aim, gamebreaker, round restart, FULL_TIME and opt-in opponent AI",
                "broader game rules and verified runtime scenarios before claiming feature completeness",
                $"current behavior is new managed gameplay, not decoded original rules; clean_room_gameplay_gate={(cleanRoomGameplayGatePassed ? "verified" : "not-present")}; authored_gameplay_runtime={(authoredGameplayRuntimeVerified ? "verified" : "not-present")}; gameplay_coverage={(gameplayCoverageVerified ? "verified" : "not-present")}; gameplay_invariants={(gameplayInvariantVerified ? "verified" : "not-present")}; authored_integrated_coverage={(authoredIntegratedCoverageVerified ? "verified" : "not-present")}; authored_round_trace_replay={(authoredRoundTraceReplayVerified ? "verified" : "not-present")}; authored_round_trace_events={roundTraceRows.Count}; authored_round_trace_has_goal={authoredRoundTraceHasGoal}; authored_round_trace_has_restart={authoredRoundTraceHasRestart}; authored_gamebreaker_trace_replay={(authoredGamebreakerTraceReplayVerified ? "verified" : "not-present")}; authored_gamebreaker_trace_events={gamebreakerTraceRows.Count}; authored_gamebreaker_trace_has_action={authoredGamebreakerTraceHasAction}; authored_gamebreaker_trace_has_goal={authoredGamebreakerTraceHasGoal}; authored_juggle_trace_replay={(authoredJuggleTraceReplayVerified ? "verified" : "not-present")}; authored_juggle_trace_events={juggleTraceRows.Count}; authored_juggle_trace_has_action={juggleTraceHasAction}; authored_juggle_trace_has_goal={juggleTraceHasGoal}; authored_panna_trace_replay={(authoredPannaTraceReplayVerified ? "verified" : "not-present")}; authored_panna_trace_events={pannaTraceRows.Count}; authored_panna_trace_has_action={authoredPannaTraceHasAction}; authored_panna_trace_has_full_time={authoredPannaTraceHasFullTime}; authored_full_time_trace_replay={(authoredFullTimeTraceReplayVerified ? "verified" : "not-present")}; authored_full_time_trace_events={fullTimeTraceRows.Count}; authored_full_time_trace_has_action={authoredFullTimeTraceHasAction}; authored_steal_trace_replay={(authoredStealTraceReplayVerified ? "verified" : "not-present")}; authored_steal_trace_events={stealTraceRows.Count}; authored_steal_trace_has_action={authoredStealTraceHasAction}; original_semantics=not-claimed"),
            new(
                "gameplay-tackle-trace",
                authoredTackleTraceReplayVerified ? "verified-authored" : "not-present",
                authoredTackleTraceReplayVerified,
                tackleTraceRows.Count,
                5,
                "authored --tackle-demo MatchTraceSerializer trace/replay with deterministic defensive possession transfer",
                "independent original defensive-control and possession evidence before treating TACKLE as decoded game semantics",
                $"authored_tackle_trace_replay={(authoredTackleTraceReplayVerified ? "verified" : "not-present")}; authored_tackle_trace_events={tackleTraceRows.Count}; authored_tackle_trace_has_action={authoredTackleTraceHasAction}; original_semantics=not-claimed"),
            new(
                "gameplay-aimed-trace",
                authoredAimedTraceReplayVerified ? "verified-authored" : "not-present",
                authoredAimedTraceReplayVerified,
                aimedTraceRows.Count,
                6,
                "authored --aimed-demo MatchTraceSerializer trace/replay with deterministic shot and goal",
                "independent original control/trajectory evidence before treating aim as decoded game semantics",
                $"authored_aimed_trace_replay={(authoredAimedTraceReplayVerified ? "verified" : "not-present")}; authored_aimed_trace_events={aimedTraceRows.Count}; authored_aimed_trace_has_shot={authoredAimedTraceHasShot}; authored_aimed_trace_has_goal={authoredAimedTraceHasGoal}; original_semantics=not-claimed"),
            new(
                "gameplay-aimed-input-trace",
                authoredAimedInputTraceReplayVerified ? "verified-authored" : "not-present",
                authoredAimedInputTraceReplayVerified,
                aimedInputTraceRows.Count,
                80,
                "authored --aimed-demo consumed input trace with frame-indexed MoveY/shoot sample",
                "independent original input mapping and trajectory evidence before treating MoveY as decoded semantics",
                $"authored_aimed_input_trace_replay={(authoredAimedInputTraceReplayVerified ? "verified" : "not-present")}; authored_aimed_input_trace_samples={aimedInputTraceRows.Count}; authored_aimed_input_trace_has_vertical_shot={authoredAimedInputTraceHasVerticalShot}; original_semantics=not-claimed"),
            new(
                "full-stack-integration",
                authoredFullStackIntegrationVerified ? "verified-authored" : "not-present",
                authoredFullStackIntegrationVerified,
                authoredFullStackIntegrationVerified ? 1 : 0,
                1,
                "single authored runtime smoke joining candidate render, gameplay, input, authored procedural animation, audio and deterministic traces",
                "repeat the same cross-system run with independently validated original bindings before changing semantic gates",
                $"integration_scope={Get(integrationReport, "integration_scope")}; candidate_material_validated={Get(integrationReport, "candidate_material_validated")}; candidate_home_enabled={Get(integrationReport, "candidate_home_enabled")}; candidate_away_enabled={Get(integrationReport, "candidate_away_enabled")}; opponent_ai_enabled={Get(integrationReport, "opponent_ai_enabled")}; rendered_frames={Get(integrationReport, "rendered_frames")}; render_checksum={Get(integrationReport, "render_checksum")}; distinct_render_checksums={Get(integrationReport, "distinct_render_checksums")}; input_samples={Get(integrationReport, "input_samples")}; animation_samples={Get(integrationReport, "animation_samples")}; match_events={Get(integrationReport, "match_events")}; audio_events={Get(integrationReport, "audio_events")}; audio_prepared={Get(integrationReport, "audio_prepared")}; audio_played={Get(integrationReport, "audio_played")}; audio_unmapped={Get(integrationReport, "audio_unmapped")}; authored_character_asset_manifest={authoredCharacterAssetManifest}; authored_scene_asset_manifest={Get(integrationReport, "authored_scene_asset_manifest")}; authored_animation_asset_manifest={Get(integrationReport, "authored_animation_asset_manifest")}; authored_audio_asset_manifest={Get(integrationReport, "authored_audio_asset_manifest")}; input_trace_written={Get(integrationReport, "input_trace_written")}; animation_trace_written={Get(integrationReport, "animation_trace_written")}; original_semantics={Get(integrationReport, "original_semantics")}"),
            new(
                "authored-integrated-coverage",
                authoredIntegratedCoverageVerified ? "verified-authored" : "not-present",
                authoredIntegratedCoverageVerified,
                ReadInt(authoredIntegratedCoverageSummary, "verified_scenario_count"),
                ReadInt(authoredIntegratedCoverageSummary, "scenario_count"),
                "nine authored scenarios each exercised rendering, authored character/scene assets, procedural animation, audio and deterministic replays",
                "independent original bindings and semantics before changing any original-asset or original-gameplay gate",
                $"scenario_count={Get(authoredIntegratedCoverageSummary, "scenario_count")}; verified_scenario_count={Get(authoredIntegratedCoverageSummary, "verified_scenario_count")}; total_rendered_frames={Get(authoredIntegratedCoverageSummary, "total_rendered_frames")}; total_audio_events={Get(authoredIntegratedCoverageSummary, "total_audio_events")}; total_animation_samples={Get(authoredIntegratedCoverageSummary, "total_animation_samples")}; original_semantics={Get(authoredIntegratedCoverageSummary, "original_semantics")}"),
            new(
                "assets-clean-room",
                cleanRoomAssetsGatePassed ? "verified-authored" : "not-present",
                cleanRoomAssetsGatePassed,
                cleanRoomAssetsGatePassed ? ReadInt(authoredIntegratedCoverageSummary, "verified_scenario_count") : 0,
                Math.Max(1, ReadInt(authoredIntegratedCoverageSummary, "scenario_count")),
                "authored scene and character manifests resolved by the managed clean-room runtime across the integrated scenario set",
                "keep authored assets separate from candidate/original mesh, texture, material, UV, skinning, rigging, morph and GPU semantics",
                $"full_stack={(authoredFullStackIntegrationVerified ? "verified" : "not-present")}; scene_assets={(authoredSceneAssetRuntimeVerified ? "verified" : "not-present")}; character_assets={(authoredCharacterRuntimeVerified ? "verified" : "not-present")}; integrated_coverage={(authoredIntegratedCoverageVerified ? "verified" : "not-present")}; original_semantics=not-claimed"),
            new(
                "rendering-clean-room",
                cleanRoomRenderingGatePassed ? "verified-authored" : "not-present",
                cleanRoomRenderingGatePassed,
                cleanRoomRenderingGatePassed ? authoredRenderedFrames : 0,
                140,
                "authored managed software framebuffer verified in headless and Win32 windowed paths across the integrated runtime",
                "keep authored raster output separate from original render-state, geometry, UV, material, skinning, rigging, morph and GPU semantics",
                $"authored_runtime={(authoredRenderingRuntimeVerified ? "verified" : "not-present")}; windowed_runtime={(windowedFrameCapture.Valid ? "verified" : "not-present")}; integrated_coverage={(authoredIntegratedCoverageVerified ? "verified" : "not-present")}; rendered_frames={authoredRenderedFrames}; original_semantics=not-claimed"),
            new(
                "audio-clean-room",
                cleanRoomAudioGatePassed ? "verified-authored" : "not-present",
                cleanRoomAudioGatePassed,
                cleanRoomAudioGatePassed ? ReadInt(audioCoverageSummary, "total_played") : 0,
                Math.Max(1, ReadInt(audioCoverageSummary, "total_events")),
                "authored cue manifest and managed audio routes prepared and played across the integrated scenario set",
                "keep authored cue policy separate from original EVT, ABKC, sample-selection and playback bindings",
                $"authored_runtime={(authoredAudioRuntimeVerified ? "verified" : "not-present")}; authored_assets={(authoredAudioAssetRuntimeVerified ? "verified" : "not-present")}; coverage={(audioCoverageVerified ? "verified" : "not-present")}; total_events={Get(audioCoverageSummary, "total_events")}; total_played={Get(audioCoverageSummary, "total_played")}; original_semantics=not-claimed"),
            new(
                "animation-clean-room",
                cleanRoomAnimationGatePassed ? "verified-authored" : "not-present",
                cleanRoomAnimationGatePassed,
                cleanRoomAnimationGatePassed ? ReadInt(authoredIntegratedCoverageSummary, "total_animation_samples") : 0,
                Math.Max(1, ReadInt(authoredIntegratedCoverageSummary, "total_animation_samples")),
                "authored animation profile manifest and procedural trace verified in the integrated managed runtime",
                "keep authored profiles separate from original AXT records, frame, pose, rigging, skinning and morph semantics",
                $"authored_runtime={(authoredAnimationRuntimeVerified ? "verified" : "not-present")}; authored_assets={(authoredAnimationAssetRuntimeVerified ? "verified" : "not-present")}; integrated_coverage={(authoredIntegratedCoverageVerified ? "verified" : "not-present")}; total_animation_samples={Get(authoredIntegratedCoverageSummary, "total_animation_samples")}; original_semantics=not-claimed"),
            new(
                "assets-candidate-runtime",
                authoredCandidateAssetRuntimeVerified ? "verified-candidate" : "not-present",
                authoredCandidateAssetRuntimeVerified,
                candidateRuntimeVerified,
                Math.Max(1, catalog.CandidateMaterials.Count),
                "full-stack authored runtime with same-trunk candidate mesh, SHPX image and managed candidate renderer",
                "independent original mesh/texture/material binding evidence before promoting candidate assets",
                $"candidate_material_validated={Get(integrationReport, "candidate_material_validated")}; candidate_home_enabled={Get(integrationReport, "candidate_home_enabled")}; candidate_away_enabled={Get(integrationReport, "candidate_away_enabled")}; candidate_source_chain_verified={(candidateSourceChainVerified ? catalog.CandidateMaterials.Count : 0)}; candidate_mesh_state_crosscheck_verified={(candidateMeshStateCrosscheckVerified ? catalog.CandidateMaterials.Count : 0)}; candidate_runtime_verified={candidateRuntimeVerified}; candidate_runtime_rejected={candidateRuntimeRejected}; managed_candidate_audit={(candidateIntegratedRuntimeAuditVerified ? "verified-candidate" : "not-present")}; original_semantics=not-claimed"),
            new(
                "assets-candidate-raw-stream-identity-control",
                originalCandidateRawStreamIdentityEvidence.Valid ? "verified-structural-negative" : "not-present",
                originalCandidateRawStreamIdentityEvidence.Valid,
                originalCandidateRawStreamIdentityEvidence.CompleteIdentityRows,
                Math.Max(1, originalCandidateRawStreamIdentityEvidence.CandidatePrimitiveRows),
                "independent raw SHA-256 re-read of every materialized candidate fetch-slot __VB payload range from the read-only ISO",
                "does not establish slot meaning, mesh draw association, UV/material interpretation, skinning, rigging, animation, morph or GPU semantics",
                $"candidate_object_count={originalCandidateRawStreamIdentityEvidence.CandidateObjectCount}; candidate_primitive_rows={originalCandidateRawStreamIdentityEvidence.CandidatePrimitiveRows}; candidate_complete_five_slot_rows={originalCandidateRawStreamIdentityEvidence.CandidateCompleteFiveSlotRows}; candidate_slot_rows={originalCandidateRawStreamIdentityEvidence.CandidateSlotRows}; raw_objects_loaded={originalCandidateRawStreamIdentityEvidence.RawObjectsLoaded}; raw_streams_loaded={originalCandidateRawStreamIdentityEvidence.RawStreamsLoaded}; raw_valid_slot_rows={originalCandidateRawStreamIdentityEvidence.RawValidSlotRows}; raw_metadata_exact_rows={originalCandidateRawStreamIdentityEvidence.RawMetadataExactRows}; raw_payload_hash_rows={originalCandidateRawStreamIdentityEvidence.RawPayloadHashRows}; complete_identity_rows={originalCandidateRawStreamIdentityEvidence.CompleteIdentityRows}; expected_unmaterialized_rows={originalCandidateRawStreamIdentityEvidence.ExpectedUnmaterializedRows}; rejected_objects={originalCandidateRawStreamIdentityEvidence.RejectedObjects}; status={originalCandidateRawStreamIdentityEvidence.Status}; provenance={originalCandidateRawStreamIdentityEvidence.Provenance}; original_semantics={originalCandidateRawStreamIdentityEvidence.OriginalSemantics}"),
            new(
                "assets-candidate-slot-category-ordinal-control",
                originalCandidateSlotCategoryControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                originalCandidateSlotCategoryControlEvidence.Valid,
                originalCandidateSlotCategoryControlEvidence.CompleteControlRows,
                Math.Max(1, originalCandidateSlotCategoryControlEvidence.CandidateRows),
                "independent lexical reparse of all candidate __GPvb category/ordinal symbols joined to the raw payload identity control",
                "does not establish slot meaning, vertex layout, UV/material interpretation, skinning, rigging, animation, morph or GPU semantics",
                $"candidate_rows={originalCandidateSlotCategoryControlEvidence.CandidateRows}; raw_identity_rows={originalCandidateSlotCategoryControlEvidence.RawIdentityRows}; candidate_object_rows={originalCandidateSlotCategoryControlEvidence.CandidateObjectRows}; raw_identity_object_rows={originalCandidateSlotCategoryControlEvidence.RawIdentityObjectRows}; joined_rows={originalCandidateSlotCategoryControlEvidence.JoinedRows}; candidate_complete_rows={originalCandidateSlotCategoryControlEvidence.CandidateCompleteRows}; raw_identity_complete_rows={originalCandidateSlotCategoryControlEvidence.RawIdentityCompleteRows}; expected_unmaterialized_rows={originalCandidateSlotCategoryControlEvidence.ExpectedUnmaterializedRows}; candidate_slot_rows={originalCandidateSlotCategoryControlEvidence.CandidateSlotRows}; symbol_format_rows={originalCandidateSlotCategoryControlEvidence.SymbolFormatRows}; category_exact_rows={originalCandidateSlotCategoryControlEvidence.CategoryExactRows}; ordinal_exact_rows={originalCandidateSlotCategoryControlEvidence.OrdinalExactRows}; complete_control_rows={originalCandidateSlotCategoryControlEvidence.CompleteControlRows}; missing_candidate_keys={originalCandidateSlotCategoryControlEvidence.MissingCandidateKeys}; missing_raw_identity_keys={originalCandidateSlotCategoryControlEvidence.MissingRawIdentityKeys}; status={originalCandidateSlotCategoryControlEvidence.Status}; original_semantics={originalCandidateSlotCategoryControlEvidence.OriginalSemantics}"),
            new(
                "rendering-candidate-runtime",
                 candidateIntegratedRuntimeAuditVerified ? "verified-candidate" : "not-present",
                 candidateIntegratedRuntimeAuditVerified,
                 candidateIntegratedRuntimeAuditVerified ? ReadInt(candidateIntegratedRuntimeAudit, "rendered_frames") : 0,
                 140,
                 "candidate opt-in full-stack runtime joined candidate geometry/image, managed raster output, authored gameplay, animation and audio",
                 "independent original render-state, geometry and UV/material evidence before changing the not-proven original-rendering gate",
                 $"candidate_asset_count={Get(candidateIntegratedRuntimeAudit, "candidate_asset_count")}; rendered_frames={Get(candidateIntegratedRuntimeAudit, "rendered_frames")}; animation_samples={Get(candidateIntegratedRuntimeAudit, "animation_samples")}; audio_events={Get(candidateIntegratedRuntimeAudit, "audio_events")}; audio_prepared={Get(candidateIntegratedRuntimeAudit, "audio_prepared")}; audio_played={Get(candidateIntegratedRuntimeAudit, "audio_played")}; audio_unmapped={Get(candidateIntegratedRuntimeAudit, "audio_unmapped")}; replay_matched={Get(candidateIntegratedRuntimeAudit, "replay_matched")}; frame_output_valid={Get(candidateIntegratedRuntimeAudit, "frame_output_valid")}; candidate_render_influence_verified={candidateRenderInfluenceVerified}; candidate_render_influence_semantics=not-claimed"),
             new(
                 "rendering-authored-runtime",
                 authoredRenderingRuntimeVerified ? "verified-authored" : "not-present",
                authoredRenderingRuntimeVerified,
                authoredRenderedFrames,
                140,
                "managed .NET software framebuffer exercised by the authored full-stack runtime",
                 "independent original render-state, geometry and UV/material evidence before changing the not-proven original-rendering gate",
                 $"rendered_frames={authoredRenderedFrames}; distinct_render_checksums={authoredDistinctRenderChecksums}; render_checksum={Get(integrationReport, "render_checksum")}; original_semantics={Get(integrationReport, "original_semantics")}"),
              new(
                  "rendering-authored-windowed-runtime",
                  windowedFrameCapture.Valid ? "verified-authored" : "not-present",
                 windowedFrameCapture.Valid,
                 windowedFrameCapture.Valid ? 1 : 0,
                 1,
                 "Release Win32 host loop produced and persisted a managed PPM framebuffer",
                  "independent original render-state, geometry and UV/material evidence before changing the not-proven original-rendering gate",
                  $"bytes={windowedFrameCapture.ByteLength}; header={windowedFrameCapture.Header}; nonuniform={windowedFrameCapture.NonUniform}; sha256={windowedFrameCapture.Sha256}; provenance=managed-software-framebuffer; original_semantics=not-claimed"),
              new(
                  "rendering-static-xbe-context",
                  staticRenderEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderEvidence.Valid,
                  staticRenderEvidence.UniqueRenderLiterals,
                  1,
                  "retail XBE render/geometry literals joined to nearby executable call context",
                  "independent function recovery and verified render-state-to-geometry/UV/material binding evidence",
                  $"render_xref_rows={staticRenderEvidence.RenderXrefRows}; unique_render_literals={staticRenderEvidence.UniqueRenderLiterals}; render_context_rows={staticRenderEvidence.RenderContextRows}; render_context_with_executable_calls={staticRenderEvidence.RenderContextWithExecutableCalls}; distinct_executable_call_targets={staticRenderEvidence.DistinctExecutableCallTargets}; category_counts={staticRenderEvidence.CategoryCounts}; status={staticRenderEvidence.Status}; provenance={staticRenderEvidence.Provenance}; original_semantics={staticRenderEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-target-windows",
                  staticRenderTargetEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderTargetEvidence.Valid,
                  staticRenderTargetEvidence.DisassembledTargets,
                  1,
                  "bounded x86 instruction windows for the ranked retail-XBE render-context targets",
                  "complete control-flow/function recovery and verified render-state-to-geometry/UV/material binding evidence",
                  $"candidate_targets={staticRenderTargetEvidence.CandidateTargets}; disassembled_targets={staticRenderTargetEvidence.DisassembledTargets}; ret_terminated_targets={staticRenderTargetEvidence.RetTerminatedTargets}; prologue_like_targets={staticRenderTargetEvidence.PrologueLikeTargets}; direct_call_count={staticRenderTargetEvidence.DirectCallCount}; indirect_call_count={staticRenderTargetEvidence.IndirectCallCount}; status={staticRenderTargetEvidence.Status}; provenance={staticRenderTargetEvidence.Provenance}; original_semantics={staticRenderTargetEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-cfg-slices",
                  staticRenderCfgEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderCfgEvidence.Valid,
                  staticRenderCfgEvidence.TargetsWithReachableCfg,
                  1,
                  "bounded reachability graph over direct branches in ranked retail-XBE render target windows",
                  "complete function recovery and verified render-state-to-geometry/UV/material binding evidence; indirect control flow remains unresolved",
                  $"candidate_targets={staticRenderCfgEvidence.CandidateTargets}; analyzed_targets={staticRenderCfgEvidence.AnalyzedTargets}; targets_with_reachable_cfg={staticRenderCfgEvidence.TargetsWithReachableCfg}; targets_with_reachable_ret={staticRenderCfgEvidence.TargetsWithReachableRet}; total_basic_blocks={staticRenderCfgEvidence.TotalBasicBlocks}; total_direct_calls={staticRenderCfgEvidence.TotalDirectCalls}; total_indirect_control_transfers={staticRenderCfgEvidence.TotalIndirectControlTransfers}; status={staticRenderCfgEvidence.Status}; provenance={staticRenderCfgEvidence.Provenance}; original_semantics={staticRenderCfgEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-call-target-slices",
                  staticRenderCallTargetEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderCallTargetEvidence.Valid,
                  staticRenderCallTargetEvidence.DisassembledTargets,
                  1,
                  "one-hop bounded slices of direct call targets reached by ranked render CFGs",
                  "complete recursive function recovery and verified render-state-to-geometry/UV/material binding evidence; calls are not followed and indirect transfers remain unresolved",
                  $"source_render_target_windows={staticRenderCallTargetEvidence.SourceRenderTargetWindows}; source_windows_with_direct_calls={staticRenderCallTargetEvidence.SourceWindowsWithDirectCalls}; unique_direct_call_targets={staticRenderCallTargetEvidence.UniqueDirectCallTargets}; mapped_executable_targets={staticRenderCallTargetEvidence.MappedExecutableTargets}; disassembled_targets={staticRenderCallTargetEvidence.DisassembledTargets}; ret_terminated_targets={staticRenderCallTargetEvidence.RetTerminatedTargets}; prologue_like_targets={staticRenderCallTargetEvidence.PrologueLikeTargets}; status={staticRenderCallTargetEvidence.Status}; provenance={staticRenderCallTargetEvidence.Provenance}; original_semantics={staticRenderCallTargetEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-call-graph",
                  staticRenderCallGraphEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderCallGraphEvidence.Valid,
                  staticRenderCallGraphEvidence.DiscoveredNodes,
                  1,
                  "bounded interprocedural graph over direct calls from static render slices",
                  "complete function recovery and verified render-state-to-geometry/UV/material binding evidence; depth and node limits remain explicit",
                  $"seed_targets={staticRenderCallGraphEvidence.SeedTargets}; seed_mapped_targets={staticRenderCallGraphEvidence.SeedMappedTargets}; max_depth={staticRenderCallGraphEvidence.MaxDepth}; discovered_nodes={staticRenderCallGraphEvidence.DiscoveredNodes}; mapped_executable_nodes={staticRenderCallGraphEvidence.MappedExecutableNodes}; disassembled_nodes={staticRenderCallGraphEvidence.DisassembledNodes}; ret_terminated_nodes={staticRenderCallGraphEvidence.RetTerminatedNodes}; nodes_with_direct_calls={staticRenderCallGraphEvidence.NodesWithDirectCalls}; unique_edges={staticRenderCallGraphEvidence.UniqueEdges}; total_edge_occurrences={staticRenderCallGraphEvidence.TotalEdgeOccurrences}; truncated_by_max_nodes={staticRenderCallGraphEvidence.TruncatedByMaxNodes}; status={staticRenderCallGraphEvidence.Status}; provenance={staticRenderCallGraphEvidence.Provenance}; original_semantics={staticRenderCallGraphEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-transfers",
                  staticRenderIndirectTransferEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderIndirectTransferEvidence.Valid,
                  staticRenderIndirectTransferEvidence.TotalIndirectTransfers,
                  1,
                  "reachable indirect call/jump operands inventoried in bounded render CFG slices",
                  "complete indirect target recovery and verified render-state-to-geometry/UV/material binding evidence; operands remain unresolved",
                  $"candidate_targets={staticRenderIndirectTransferEvidence.CandidateTargets}; analyzed_targets={staticRenderIndirectTransferEvidence.AnalyzedTargets}; targets_with_indirect_transfers={staticRenderIndirectTransferEvidence.TargetsWithIndirectTransfers}; total_indirect_transfers={staticRenderIndirectTransferEvidence.TotalIndirectTransfers}; indirect_calls={staticRenderIndirectTransferEvidence.IndirectCalls}; indirect_jumps={staticRenderIndirectTransferEvidence.IndirectJumps}; register_operands={staticRenderIndirectTransferEvidence.RegisterOperands}; memory_operands={staticRenderIndirectTransferEvidence.MemoryOperands}; absolute_memory_operands={staticRenderIndirectTransferEvidence.AbsoluteMemoryOperands}; distinct_operand_shapes={staticRenderIndirectTransferEvidence.DistinctOperandShapes}; status={staticRenderIndirectTransferEvidence.Status}; provenance={staticRenderIndirectTransferEvidence.Provenance}; original_semantics={staticRenderIndirectTransferEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-reference-words",
                  staticRenderIndirectReferenceEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderIndirectReferenceEvidence.Valid,
                  staticRenderIndirectReferenceEvidence.ReferenceWordsValid,
                  1,
                  "bounded words read at base-less absolute indirect references and indexed table bases",
                  "independent pointer/function recovery and verified render-state-to-geometry/UV/material binding evidence; words remain candidates",
                  $"indirect_rows={staticRenderIndirectReferenceEvidence.IndirectRows}; reference_rows={staticRenderIndirectReferenceEvidence.ReferenceRows}; absolute_memory_rows={staticRenderIndirectReferenceEvidence.AbsoluteMemoryRows}; indexed_table_base_rows={staticRenderIndirectReferenceEvidence.IndexedTableBaseRows}; bounded_reference_reads={staticRenderIndirectReferenceEvidence.BoundedReferenceReads}; reference_words_valid={staticRenderIndirectReferenceEvidence.ReferenceWordsValid}; reference_words_in_executable_sections={staticRenderIndirectReferenceEvidence.ReferenceWordsInExecutableSections}; distinct_reference_addresses={staticRenderIndirectReferenceEvidence.DistinctReferenceAddresses}; distinct_raw_words={staticRenderIndirectReferenceEvidence.DistinctRawWords}; status={staticRenderIndirectReferenceEvidence.Status}; provenance={staticRenderIndirectReferenceEvidence.Provenance}; original_semantics={staticRenderIndirectReferenceEvidence.OriginalSemantics}"),
               new(
                   "rendering-static-xbe-indirect-reference-word-slices",
                   staticRenderIndirectReferenceSliceEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderIndirectReferenceSliceEvidence.Valid,
                  staticRenderIndirectReferenceSliceEvidence.DisassembledCandidateWords,
                  1,
                   "bounded executable-section slices beginning at raw words from the indirect-reference audit",
                   "independent function-boundary/pointer recovery and verified render-state-to-geometry/UV/material binding evidence; slices remain candidates",
                   $"reference_rows={staticRenderIndirectReferenceSliceEvidence.ReferenceRows}; distinct_raw_words={staticRenderIndirectReferenceSliceEvidence.DistinctRawWords}; executable_raw_words={staticRenderIndirectReferenceSliceEvidence.ExecutableRawWords}; disassembled_candidate_words={staticRenderIndirectReferenceSliceEvidence.DisassembledCandidateWords}; ret_terminated_candidate_words={staticRenderIndirectReferenceSliceEvidence.RetTerminatedCandidateWords}; prologue_like_candidate_words={staticRenderIndirectReferenceSliceEvidence.PrologueLikeCandidateWords}; status={staticRenderIndirectReferenceSliceEvidence.Status}; provenance={staticRenderIndirectReferenceSliceEvidence.Provenance}; original_semantics={staticRenderIndirectReferenceSliceEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-window-join",
                  staticRenderIndirectWindowJoinEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderIndirectWindowJoinEvidence.Valid,
                  staticRenderIndirectWindowJoinEvidence.TargetWindows,
                  1,
                  "exact join of bounded render windows with indirect transfer, reference-word and candidate-slice reports",
                  "independent pointer/function recovery and verified render-state-to-geometry/UV/material binding evidence; the join remains structural",
                  $"target_windows={staticRenderIndirectWindowJoinEvidence.TargetWindows}; targets_with_indirect_transfers={staticRenderIndirectWindowJoinEvidence.TargetsWithIndirectTransfers}; total_indirect_transfers={staticRenderIndirectWindowJoinEvidence.TotalIndirectTransfers}; indirect_calls={staticRenderIndirectWindowJoinEvidence.IndirectCalls}; indirect_jumps={staticRenderIndirectWindowJoinEvidence.IndirectJumps}; targets_with_references={staticRenderIndirectWindowJoinEvidence.TargetsWithReferences}; reference_rows={staticRenderIndirectWindowJoinEvidence.ReferenceRows}; targets_with_transfer_reference_instruction_overlap={staticRenderIndirectWindowJoinEvidence.TargetsWithTransferReferenceInstructionOverlap}; exact_transfer_reference_instruction_rows={staticRenderIndirectWindowJoinEvidence.ExactTransferReferenceInstructionRows}; distinct_raw_words={staticRenderIndirectWindowJoinEvidence.DistinctRawWords}; executable_raw_words={staticRenderIndirectWindowJoinEvidence.ExecutableRawWords}; disassembled_candidate_words={staticRenderIndirectWindowJoinEvidence.DisassembledCandidateWords}; ret_terminated_candidate_words={staticRenderIndirectWindowJoinEvidence.RetTerminatedCandidateWords}; prologue_like_candidate_words={staticRenderIndirectWindowJoinEvidence.PrologueLikeCandidateWords}; status={staticRenderIndirectWindowJoinEvidence.Status}; original_semantics={staticRenderIndirectWindowJoinEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-indexed-table",
                  staticRenderIndirectIndexedTableEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderIndirectIndexedTableEvidence.Valid,
                  staticRenderIndirectIndexedTableEvidence.ExecutableTableWords,
                  1,
                  "bounded scan of the indexed indirect-transfer table base, classified only by executable-section membership",
                  "independent table length/pointer/function recovery and verified render-state-to-geometry/UV/material binding evidence; the scan remains bounded and unresolved",
                  $"indexed_rows={staticRenderIndirectIndexedTableEvidence.IndexedRows}; distinct_table_bases={staticRenderIndirectIndexedTableEvidence.DistinctTableBases}; tables_scan_capped={staticRenderIndirectIndexedTableEvidence.TablesScanCapped}; tables_terminated_after_non_executable_words={staticRenderIndirectIndexedTableEvidence.TablesTerminatedAfterNonExecutableWords}; readable_table_words={staticRenderIndirectIndexedTableEvidence.ReadableTableWords}; executable_table_words={staticRenderIndirectIndexedTableEvidence.ExecutableTableWords}; non_executable_table_words={staticRenderIndirectIndexedTableEvidence.NonExecutableTableWords}; unreadable_table_words={staticRenderIndirectIndexedTableEvidence.UnreadableTableWords}; distinct_executable_words={staticRenderIndirectIndexedTableEvidence.DistinctExecutableWords}; tables_with_contiguous_executable_prefix={staticRenderIndirectIndexedTableEvidence.TablesWithContiguousExecutablePrefix}; max_contiguous_executable_prefix={staticRenderIndirectIndexedTableEvidence.MaxContiguousExecutablePrefix}; status={staticRenderIndirectIndexedTableEvidence.Status}; provenance={staticRenderIndirectIndexedTableEvidence.Provenance}; original_semantics={staticRenderIndirectIndexedTableEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-indexed-targets",
                  staticRenderIndirectIndexedTargetEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderIndirectIndexedTargetEvidence.Valid,
                  staticRenderIndirectIndexedTargetEvidence.DisassembledCandidateTargets,
                  1,
                  "bounded executable-section slices at distinct candidates from the indexed indirect-transfer table",
                  "independent table length/pointer/function recovery and verified render-state-to-geometry/UV/material binding evidence; target windows remain candidates",
                  $"indexed_table_rows={staticRenderIndirectIndexedTargetEvidence.IndexedTableRows}; candidate_entry_occurrences={staticRenderIndirectIndexedTargetEvidence.CandidateEntryOccurrences}; distinct_candidate_targets={staticRenderIndirectIndexedTargetEvidence.DistinctCandidateTargets}; mapped_executable_targets={staticRenderIndirectIndexedTargetEvidence.MappedExecutableTargets}; disassembled_candidate_targets={staticRenderIndirectIndexedTargetEvidence.DisassembledCandidateTargets}; ret_terminated_candidate_targets={staticRenderIndirectIndexedTargetEvidence.RetTerminatedCandidateTargets}; prologue_like_candidate_targets={staticRenderIndirectIndexedTargetEvidence.PrologueLikeCandidateTargets}; status={staticRenderIndirectIndexedTargetEvidence.Status}; provenance={staticRenderIndirectIndexedTargetEvidence.Provenance}; original_semantics={staticRenderIndirectIndexedTargetEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-indexed-accessor-shapes",
                  staticRenderIndirectIndexedAccessorEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderIndirectIndexedAccessorEvidence.Valid,
                  staticRenderIndirectIndexedAccessorEvidence.DisassembledCandidateTargets,
                  81,
                  "bounded instruction-shape evidence through the first observed return of indexed-table candidates",
                  "independent pointer/function recovery and render-state-to-geometry/UV/material evidence; instruction shapes remain opaque and do not define fields or semantics",
                  $"indexed_table_rows={staticRenderIndirectIndexedAccessorEvidence.IndexedTableRows}; candidate_entry_occurrences={staticRenderIndirectIndexedAccessorEvidence.CandidateEntryOccurrences}; distinct_candidate_targets={staticRenderIndirectIndexedAccessorEvidence.DistinctCandidateTargets}; mapped_executable_targets={staticRenderIndirectIndexedAccessorEvidence.MappedExecutableTargets}; disassembled_candidate_targets={staticRenderIndirectIndexedAccessorEvidence.DisassembledCandidateTargets}; ret_terminated_candidate_targets={staticRenderIndirectIndexedAccessorEvidence.RetTerminatedCandidateTargets}; fallback_pop_pop_ret_shapes={staticRenderIndirectIndexedAccessorEvidence.FallbackPopPopRetShapes}; single_mov_memory_ret_shapes={staticRenderIndirectIndexedAccessorEvidence.SingleMovMemoryRetShapes}; lea_push_call_ret_shapes={staticRenderIndirectIndexedAccessorEvidence.LeaPushCallRetShapes}; other_shapes={staticRenderIndirectIndexedAccessorEvidence.OtherShapes}; distinct_memory_displacements={staticRenderIndirectIndexedAccessorEvidence.DistinctMemoryDisplacements}; distinct_direct_call_targets={staticRenderIndirectIndexedAccessorEvidence.DistinctDirectCallTargets}; status={staticRenderIndirectIndexedAccessorEvidence.Status}; provenance={staticRenderIndirectIndexedAccessorEvidence.Provenance}; original_semantics={staticRenderIndirectIndexedAccessorEvidence.OriginalSemantics}"),
              new(
                  "rendering-candidate-fetch-record-association",
                  candidateFetchAssociationEvidence.Valid ? "verified-structural" : "not-present",
                  candidateFetchAssociationEvidence.Valid,
                  candidateFetchAssociationEvidence.ValidSlotRows,
                  Math.Max(1, candidateFetchAssociationEvidence.SlotRows),
                  "same-trunk candidate objects with exact observed fetch-slot metadata joined to validated __VB record stride/length metadata",
                  "independent original render-state, pointer/function, geometry and UV/material evidence; slot records remain opaque and candidate-only",
                  $"candidate_count={candidateFetchAssociationEvidence.CandidateCount}; candidate_assets_loaded={candidateFetchAssociationEvidence.CandidateAssetsLoaded}; candidate_assets_with_complete_five_slot_layouts={candidateFetchAssociationEvidence.CandidateAssetsWithCompleteFiveSlotLayouts}; primitive_rows={candidateFetchAssociationEvidence.PrimitiveRows}; complete_five_slot_primitive_rows={candidateFetchAssociationEvidence.CompleteFiveSlotPrimitiveRows}; slot_rows={candidateFetchAssociationEvidence.SlotRows}; valid_slot_rows={candidateFetchAssociationEvidence.ValidSlotRows}; rejected_candidates={candidateFetchAssociationEvidence.RejectedCandidates}; status={candidateFetchAssociationEvidence.Status}; provenance={candidateFetchAssociationEvidence.Provenance}; original_semantics={candidateFetchAssociationEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-render-chain-structure",
                  originalRenderChainEvidence.Valid ? "verified-structural" : "not-present",
                  originalRenderChainEvidence.Valid,
                  originalRenderChainEvidence.ValidChainRows,
                  Math.Max(1, originalRenderChainEvidence.PrimitiveRows),
                  "same-trunk raw EAGL objects preserve the model descriptor, render-method, global-parameter and opaque command-stream chain without candidate-mesh input",
                  "independent original pointer/function, geometry and UV/material evidence; command IDs and state text remain semantically undecoded",
                  $"model_package_candidates={originalRenderChainEvidence.ModelPackageCandidates}; model_packages_loaded={originalRenderChainEvidence.ModelPackagesLoaded}; object_candidates={originalRenderChainEvidence.ObjectCandidates}; objects_loaded={originalRenderChainEvidence.ObjectsLoaded}; primitive_rows={originalRenderChainEvidence.PrimitiveRows}; valid_chain_rows={originalRenderChainEvidence.ValidChainRows}; complete_command_streams={originalRenderChainEvidence.CompleteCommandStreams}; state_relocation_rows={originalRenderChainEvidence.StateRelocationRows}; shapename_state_rows={originalRenderChainEvidence.ShapeNameStateRows}; primitive_type_state_rows={originalRenderChainEvidence.PrimitiveTypeStateRows}; fetch_slot_rows={originalRenderChainEvidence.FetchSlotRows}; complete_five_slot_fetch_rows={originalRenderChainEvidence.CompleteFiveSlotFetchRows}; state_fetch_overlap_rows={originalRenderChainEvidence.StateFetchOverlapRows}; state_shapename_primitive_type_overlap_rows={originalRenderChainEvidence.StateShapePrimitiveTypeOverlapRows}; rejected_packages={originalRenderChainEvidence.RejectedPackages}; rejected_objects={originalRenderChainEvidence.RejectedObjects}; status={originalRenderChainEvidence.Status}; provenance={originalRenderChainEvidence.Provenance}; original_semantics={originalRenderChainEvidence.OriginalSemantics}; state_fetch_control_status={originalRenderChainEvidence.StateFetchControlStatus}"),
              new(
                  "rendering-original-command-id-dispatch-control",
                  originalRenderCommandDispatchEvidence.Valid ? "verified-structural" : "not-present",
                  originalRenderCommandDispatchEvidence.Valid,
                  originalRenderCommandDispatchEvidence.MappedObservedCommandIds,
                  Math.Max(1, originalRenderCommandDispatchEvidence.UniqueObservedCommandIds),
                  "independent XBE static decode joins every observed complete EAGL command ID to the indexed executable handler-table slot",
                  "handler behavior and command meaning remain undecoded; no state-to-draw, UV, material, skinning, rigging, animation, morph or GPU binding is claimed",
                  $"xbe_sha256={originalRenderCommandDispatchEvidence.XbeSha256}; eagl_report_sha256={originalRenderCommandDispatchEvidence.EaglReportSha256}; eagl_rows={originalRenderCommandDispatchEvidence.EaglRows}; complete_command_stream_rows={originalRenderCommandDispatchEvidence.CompleteCommandStreamRows}; command_occurrences={originalRenderCommandDispatchEvidence.CommandOccurrences}; unique_observed_command_ids={originalRenderCommandDispatchEvidence.UniqueObservedCommandIds}; mapped_observed_command_ids={originalRenderCommandDispatchEvidence.MappedObservedCommandIds}; executable_dispatch_table_prefix_entries={originalRenderCommandDispatchEvidence.ExecutableDispatchTablePrefixEntries}; distinct_dispatch_targets={originalRenderCommandDispatchEvidence.DistinctDispatchTargets}; dispatch_table_base={originalRenderCommandDispatchEvidence.DispatchTableBase}; first_non_executable_index={originalRenderCommandDispatchEvidence.FirstNonExecutableIndex}; first_non_executable_word={originalRenderCommandDispatchEvidence.FirstNonExecutableWord}; dispatch_binding={originalRenderCommandDispatchEvidence.DispatchBinding}; semantic_binding={originalRenderCommandDispatchEvidence.SemanticBinding}; status={originalRenderCommandDispatchEvidence.Status}; provenance={originalRenderCommandDispatchEvidence.Provenance}; original_semantics={originalRenderCommandDispatchEvidence.OriginalSemantics}"),
              ReadRenderParameterControl(Path.Combine(reportRoot, "fifa_pc")),
              ReadIndexedDrawSourceControl(Path.Combine(reportRoot, "fifa_pc")),
              ReadMaterialDeclarationControl(Path.Combine(reportRoot, "fifa_pc")),
              ReadVertexProgramControl(Path.Combine(reportRoot, "fifa_pc")),
              ReadModelMaterialReferenceControl(Path.Combine(reportRoot, "fifa_pc")),
              new(
                  "rendering-original-vertex-record-structure",
                  string.Equals(originalRenderChainEvidence.VertexRecordControlStatus, EaglOriginalRenderChainAudit.VertexRecordControlStatus, StringComparison.Ordinal) ? "verified-structural" : "not-present",
                  string.Equals(originalRenderChainEvidence.VertexRecordControlStatus, EaglOriginalRenderChainAudit.VertexRecordControlStatus, StringComparison.Ordinal),
                  originalRenderChainEvidence.ValidatedVertexStreamRows,
                  Math.Max(1, originalRenderChainEvidence.ObjectCandidates),
                  "same-trunk raw ELF symbol and validated __VB record inventory alongside the __Model render-chain rows",
                  "does not establish primitive-to-stream semantic binding or assign geometry, UV, material, skinning, rigging, animation, morph or GPU meaning",
                  $"model_symbol_rows={originalRenderChainEvidence.ModelSymbolRows}; bounds_symbol_rows={originalRenderChainEvidence.BoundsSymbolRows}; render_method_symbol_rows={originalRenderChainEvidence.RenderMethodSymbolRows}; vertex_buffer_symbol_rows={originalRenderChainEvidence.VertexBufferSymbolRows}; validated_vertex_stream_rows={originalRenderChainEvidence.ValidatedVertexStreamRows}; objects_with_bounds={originalRenderChainEvidence.ObjectsWithBounds}; objects_with_validated_vertex_streams={originalRenderChainEvidence.ObjectsWithValidatedVertexStreams}; status={originalRenderChainEvidence.VertexRecordControlStatus}; provenance={originalRenderChainEvidence.Provenance}; original_semantics={originalRenderChainEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-opaque-vertex-record-association",
                  originalVertexRecordAssociationEvidence.Valid ? "verified-structural" : "not-present",
                  originalVertexRecordAssociationEvidence.Valid,
                  originalVertexRecordAssociationEvidence.RowsWithUniqueOpaqueSequences,
                  Math.Max(1, originalVertexRecordAssociationEvidence.PrimitiveRows),
                  "same-trunk original EAGL path: opaque category/ordinal __VB symbol keys uniquely match the observed five-slot stride sequence",
                  "does not establish attribute, geometry, UV, material, skinning, rigging, animation, morph or GPU semantics; the association remains structural",
                  $"model_package_candidates={originalVertexRecordAssociationEvidence.ModelPackageCandidates}; model_packages_loaded={originalVertexRecordAssociationEvidence.ModelPackagesLoaded}; object_candidates={originalVertexRecordAssociationEvidence.ObjectCandidates}; objects_loaded={originalVertexRecordAssociationEvidence.ObjectsLoaded}; validated_vertex_stream_rows={originalVertexRecordAssociationEvidence.ValidatedVertexStreamRows}; objects_with_validated_vertex_streams={originalVertexRecordAssociationEvidence.ObjectsWithValidatedVertexStreams}; primitive_rows={originalVertexRecordAssociationEvidence.PrimitiveRows}; rows_with_opaque_ordinal_records={originalVertexRecordAssociationEvidence.RowsWithOpaqueOrdinalRecords}; rows_with_unique_opaque_sequences={originalVertexRecordAssociationEvidence.RowsWithUniqueOpaqueSequences}; complete_five_slot_fetch_rows={originalVertexRecordAssociationEvidence.CompleteFiveSlotFetchRows}; status={originalVertexRecordAssociationEvidence.Status}; provenance={originalVertexRecordAssociationEvidence.Provenance}; original_semantics={originalVertexRecordAssociationEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-opaque-vertex-record-association-control",
                  originalVertexRecordAssociationControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalVertexRecordAssociationControlEvidence.Valid,
                  originalVertexRecordAssociationControlEvidence.RecomputedExactRows,
                  Math.Max(1, originalVertexRecordAssociationControlEvidence.InputUniqueRows),
                  "independent TSV reparse reproduces the exact opaque stride/category sequence while reversed-order and shifted-ordinal controls remain empty",
                  "does not establish attribute, geometry, UV, material, skinning, rigging, animation, morph or GPU semantics",
                  $"input_rows={originalVertexRecordAssociationControlEvidence.InputRows}; object_rows={originalVertexRecordAssociationControlEvidence.ObjectRows}; input_unique_rows={originalVertexRecordAssociationControlEvidence.InputUniqueRows}; recomputed_exact_rows={originalVertexRecordAssociationControlEvidence.RecomputedExactRows}; reverse_stride_match_rows={originalVertexRecordAssociationControlEvidence.ReverseStrideMatchRows}; shifted_ordinal_match_rows={originalVertexRecordAssociationControlEvidence.ShiftedOrdinalMatchRows}; complete_five_slot_rows={originalVertexRecordAssociationControlEvidence.CompleteFiveSlotRows}; status={originalVertexRecordAssociationControlEvidence.Status}; provenance={originalVertexRecordAssociationControlEvidence.Provenance}; original_semantics={originalVertexRecordAssociationControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-numeric-vertex-payload-structure",
                  originalVertexPayloadEvidence.Valid ? "verified-structural" : "not-present",
                  originalVertexPayloadEvidence.Valid,
                  originalVertexPayloadEvidence.ValidatedVertexStreamRows,
                  Math.Max(1, originalVertexPayloadEvidence.ObjectsLoaded),
                  "same-trunk raw original EAGL __VB payloads with validated ranges, finite numeric profiles and declared-bounds containment counts",
                  "does not assign position, UV, material, skinning, rigging, animation, morph or GPU semantics; numeric profiles remain structural",
                  $"model_package_candidates={originalVertexPayloadEvidence.ModelPackageCandidates}; model_packages_loaded={originalVertexPayloadEvidence.ModelPackagesLoaded}; object_candidates={originalVertexPayloadEvidence.ObjectCandidates}; objects_loaded={originalVertexPayloadEvidence.ObjectsLoaded}; validated_vertex_stream_rows={originalVertexPayloadEvidence.ValidatedVertexStreamRows}; objects_with_validated_vertex_streams={originalVertexPayloadEvidence.ObjectsWithValidatedVertexStreams}; objects_with_finite_packed_float_profiles={originalVertexPayloadEvidence.ObjectsWithFinitePackedFloatProfiles}; objects_with_finite_float2_profiles={originalVertexPayloadEvidence.ObjectsWithFiniteFloat2Profiles}; objects_with_declared_bounds_containment={originalVertexPayloadEvidence.ObjectsWithDeclaredBoundsContainment}; payload_rows={originalVertexPayloadEvidence.PayloadRows}; objects_with_distinct_payload_ranges={originalVertexPayloadEvidence.ObjectsWithDistinctPayloadRanges}; objects_with_overlapping_payload_ranges={originalVertexPayloadEvidence.ObjectsWithOverlappingPayloadRanges}; status={originalVertexPayloadEvidence.Status}; provenance={originalVertexPayloadEvidence.Provenance}; original_semantics={originalVertexPayloadEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-numeric-vertex-payload-control",
                  originalVertexPayloadControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalVertexPayloadControlEvidence.Valid,
                  originalVertexPayloadControlEvidence.NumericSignatureMatchRows,
                  Math.Max(1, originalVertexPayloadControlEvidence.InputRows),
                  "independent TSV reparse reproduces the numeric signature counts and validates each payload-range counter without assigning field semantics",
                  "does not assign position, geometry, UV, material, skinning, rigging, animation, morph or GPU semantics; control remains observational",
                  $"input_rows={originalVertexPayloadControlEvidence.InputRows}; object_rows={originalVertexPayloadControlEvidence.ObjectRows}; numeric_signature_match_rows={originalVertexPayloadControlEvidence.NumericSignatureMatchRows}; payload_range_metadata_valid_rows={originalVertexPayloadControlEvidence.PayloadRangeMetadataValidRows}; input_status_valid_rows={originalVertexPayloadControlEvidence.InputStatusValidRows}; status={originalVertexPayloadControlEvidence.Status}; provenance={originalVertexPayloadControlEvidence.Provenance}; original_semantics={originalVertexPayloadControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-numeric-stream-ambiguity-control",
                  originalNumericStreamAmbiguityEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalNumericStreamAmbiguityEvidence.Valid,
                  originalNumericStreamAmbiguityEvidence.InputRows,
                  Math.Max(1, originalNumericStreamAmbiguityEvidence.InputRows),
                  "independent object-level cardinality control classifies declared-bounds matches as none, unique or multiple without assigning a stream attribute",
                  "does not establish position, geometry, UV, material, skinning, rigging, animation, morph or GPU semantics; unique object-level counts remain not-proven bindings",
                  $"input_rows={originalNumericStreamAmbiguityEvidence.InputRows}; valid_input_rows={originalNumericStreamAmbiguityEvidence.ValidInputRows}; objects_with_no_bounds_match={originalNumericStreamAmbiguityEvidence.ObjectsWithNoBoundsMatch}; objects_with_unique_bounds_match={originalNumericStreamAmbiguityEvidence.ObjectsWithUniqueBoundsMatch}; objects_with_multiple_bounds_matches={originalNumericStreamAmbiguityEvidence.ObjectsWithMultipleBoundsMatches}; objects_with_non_unique_or_missing_match={originalNumericStreamAmbiguityEvidence.ObjectsWithNonUniqueOrMissingMatch}; bounds_match_cardinality_histogram={originalNumericStreamAmbiguityEvidence.BoundsMatchCardinalityHistogram}; semantic_binding={originalNumericStreamAmbiguityEvidence.SemanticBinding}; status={originalNumericStreamAmbiguityEvidence.Status}; provenance={originalNumericStreamAmbiguityEvidence.Provenance}; original_semantics={originalNumericStreamAmbiguityEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-bounds-consistent-record-control",
                  originalBoundsConsistentRecordEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalBoundsConsistentRecordEvidence.Valid,
                  originalBoundsConsistentRecordEvidence.ObjectOrdinalGroups,
                  Math.Max(1, originalBoundsConsistentRecordEvidence.ObjectOrdinalGroups),
                  "independent opaque object/ordinal grouping shows the first raw category record is the only bounds-consistent record in 2363/2364 complete groups",
                  "does not call the first raw record position or geometry and does not establish UV, material, skinning, rigging, animation, morph or GPU semantics",
                  $"input_rows={originalBoundsConsistentRecordEvidence.InputRows}; object_ordinal_groups={originalBoundsConsistentRecordEvidence.ObjectOrdinalGroups}; complete_five_stream_groups={originalBoundsConsistentRecordEvidence.CompleteFiveStreamGroups}; category_sequence_valid_groups={originalBoundsConsistentRecordEvidence.CategorySequenceValidGroups}; valid_stream_rows={originalBoundsConsistentRecordEvidence.ValidStreamRows}; first_raw_stride12_groups={originalBoundsConsistentRecordEvidence.FirstRawStride12Groups}; first_raw_bounds_match_groups={originalBoundsConsistentRecordEvidence.FirstRawBoundsMatchGroups}; first_raw_no_bounds_match_groups={originalBoundsConsistentRecordEvidence.FirstRawNoBoundsMatchGroups}; later_raw_bounds_match_groups={originalBoundsConsistentRecordEvidence.LaterRawBoundsMatchGroups}; all_bounds_matches_are_first_raw={originalBoundsConsistentRecordEvidence.AllBoundsMatchesAreFirstRaw}; first_raw_category_histogram={originalBoundsConsistentRecordEvidence.FirstRawCategoryHistogram}; semantic_binding={originalBoundsConsistentRecordEvidence.SemanticBinding}; status={originalBoundsConsistentRecordEvidence.Status}; provenance={originalBoundsConsistentRecordEvidence.Provenance}; original_semantics={originalBoundsConsistentRecordEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-bounds-fetch-slot-join-control",
                  originalBoundsFetchSlotJoinEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalBoundsFetchSlotJoinEvidence.Valid,
                  originalBoundsFetchSlotJoinEvidence.ObjectOrdinalGroups,
                  Math.Max(1, originalBoundsFetchSlotJoinEvidence.ObjectOrdinalGroups),
                  "independent key join maps the first raw category record to opaque fetch slot 0 in every complete group while preserving the two incomplete slot cases",
                  "does not call fetch slot 0 position or geometry and does not establish UV, material, skinning, rigging, animation, morph or GPU semantics",
                  $"numeric_rows={originalBoundsFetchSlotJoinEvidence.NumericRows}; topology_rows={originalBoundsFetchSlotJoinEvidence.TopologyRows}; joined_rows={originalBoundsFetchSlotJoinEvidence.JoinedRows}; missing_numeric_keys={originalBoundsFetchSlotJoinEvidence.MissingNumericKeys}; missing_topology_keys={originalBoundsFetchSlotJoinEvidence.MissingTopologyKeys}; object_ordinal_groups={originalBoundsFetchSlotJoinEvidence.ObjectOrdinalGroups}; complete_five_stream_groups={originalBoundsFetchSlotJoinEvidence.CompleteFiveStreamGroups}; identity_join_rows={originalBoundsFetchSlotJoinEvidence.IdentityJoinRows}; numeric_input_status_valid_rows={originalBoundsFetchSlotJoinEvidence.NumericInputStatusValidRows}; topology_input_status_valid_rows={originalBoundsFetchSlotJoinEvidence.TopologyInputStatusValidRows}; first_raw_fetch_slot_zero_groups={originalBoundsFetchSlotJoinEvidence.FirstRawFetchSlotZeroGroups}; first_raw_stride_match_groups={originalBoundsFetchSlotJoinEvidence.FirstRawStrideMatchGroups}; first_raw_bounds_match_groups={originalBoundsFetchSlotJoinEvidence.FirstRawBoundsMatchGroups}; first_raw_no_bounds_match_groups={originalBoundsFetchSlotJoinEvidence.FirstRawNoBoundsMatchGroups}; later_raw_bounds_match_groups={originalBoundsFetchSlotJoinEvidence.LaterRawBoundsMatchGroups}; incomplete_fetch_slot_groups={originalBoundsFetchSlotJoinEvidence.IncompleteFetchSlotGroups}; semantic_binding={originalBoundsFetchSlotJoinEvidence.SemanticBinding}; status={originalBoundsFetchSlotJoinEvidence.Status}; provenance={originalBoundsFetchSlotJoinEvidence.Provenance}; original_semantics={originalBoundsFetchSlotJoinEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-numeric-slot-role-discrimination-control",
                  originalNumericSlotRoleDiscriminationEvidence.Valid ? "verified-structural" : "not-present",
                  originalNumericSlotRoleDiscriminationEvidence.Valid,
                  originalNumericSlotRoleDiscriminationEvidence.CompleteGroupsWithUniqueNearUnitFloat2,
                  Math.Max(1, originalNumericSlotRoleDiscriminationEvidence.CompleteFiveSlotGroups),
                  "independent TSV-keyed join reproduces the original per-primitive slot/cardinality pattern: every complete group has exactly one near-unit float2 profile at raw slot 1, with no other complete slot carrying that profile",
                  "does not call slot 1 UV, slot 0 position, or any stream attribute and does not establish state-to-draw, material, skinning, rigging, animation, morph or GPU semantics",
                  $"numeric_rows={originalNumericSlotRoleDiscriminationEvidence.NumericRows}; topology_rows={originalNumericSlotRoleDiscriminationEvidence.TopologyRows}; joined_rows={originalNumericSlotRoleDiscriminationEvidence.JoinedRows}; per_primitive_slot_rows={originalNumericSlotRoleDiscriminationEvidence.PerPrimitiveSlotRows}; missing_numeric_keys={originalNumericSlotRoleDiscriminationEvidence.MissingNumericKeys}; missing_topology_keys={originalNumericSlotRoleDiscriminationEvidence.MissingTopologyKeys}; duplicate_numeric_keys={originalNumericSlotRoleDiscriminationEvidence.DuplicateNumericKeys}; duplicate_topology_keys={originalNumericSlotRoleDiscriminationEvidence.DuplicateTopologyKeys}; primitive_groups={originalNumericSlotRoleDiscriminationEvidence.PrimitiveGroups}; groups_with_five_rows={originalNumericSlotRoleDiscriminationEvidence.GroupsWithFiveRows}; complete_five_slot_groups={originalNumericSlotRoleDiscriminationEvidence.CompleteFiveSlotGroups}; complete_groups_with_unique_near_unit_float2={originalNumericSlotRoleDiscriminationEvidence.CompleteGroupsWithUniqueNearUnitFloat2}; complete_groups_with_near_unit_float2={originalNumericSlotRoleDiscriminationEvidence.CompleteGroupsWithNearUnitFloat2}; complete_groups_with_slot1_near_unit_float2={originalNumericSlotRoleDiscriminationEvidence.CompleteGroupsWithSlot1NearUnitFloat2}; complete_groups_with_other_slot_near_unit_float2={originalNumericSlotRoleDiscriminationEvidence.CompleteGroupsWithOtherSlotNearUnitFloat2}; complete_groups_with_slot0_bounds_match={originalNumericSlotRoleDiscriminationEvidence.CompleteGroupsWithSlot0BoundsMatch}; complete_groups_with_no_slot0_bounds_match={originalNumericSlotRoleDiscriminationEvidence.CompleteGroupsWithNoSlot0BoundsMatch}; incomplete_five_slot_groups={originalNumericSlotRoleDiscriminationEvidence.IncompleteFiveSlotGroups}; numeric_input_status_valid_rows={originalNumericSlotRoleDiscriminationEvidence.NumericInputStatusValidRows}; topology_input_status_valid_rows={originalNumericSlotRoleDiscriminationEvidence.TopologyInputStatusValidRows}; slot_row_count_histogram={originalNumericSlotRoleDiscriminationEvidence.SlotRowCountHistogram}; near_unit_float2_slot_histogram={originalNumericSlotRoleDiscriminationEvidence.NearUnitFloat2SlotHistogram}; semantic_binding={originalNumericSlotRoleDiscriminationEvidence.SemanticBinding}; status={originalNumericSlotRoleDiscriminationEvidence.Status}; provenance={originalNumericSlotRoleDiscriminationEvidence.Provenance}; original_semantics={originalNumericSlotRoleDiscriminationEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-numeric-slot-cooccurrence-control",
                  originalStateNumericSlotCooccurrenceEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateNumericSlotCooccurrenceEvidence.Valid,
                  originalStateNumericSlotCooccurrenceEvidence.CompleteGroupsWithSlot1NearUnitFloat2,
                  Math.Max(1, originalStateNumericSlotCooccurrenceEvidence.CompleteFiveSlotGroups),
                  "independent primitive-key join cross-checks original state/fetch rows against the numeric slot-role report while preserving opaque state labels and the two incomplete slot groups",
                  "does not establish state-to-draw, state-to-geometry, slot meaning, UV, position, material, skinning, rigging, animation, morph or GPU semantics",
                  $"state_rows={originalStateNumericSlotCooccurrenceEvidence.StateRows}; role_rows={originalStateNumericSlotCooccurrenceEvidence.RoleRows}; joined_rows={originalStateNumericSlotCooccurrenceEvidence.JoinedRows}; missing_state_keys={originalStateNumericSlotCooccurrenceEvidence.MissingStateKeys}; missing_role_keys={originalStateNumericSlotCooccurrenceEvidence.MissingRoleKeys}; duplicate_state_keys={originalStateNumericSlotCooccurrenceEvidence.DuplicateStateKeys}; duplicate_role_keys={originalStateNumericSlotCooccurrenceEvidence.DuplicateRoleKeys}; state_input_status_valid_rows={originalStateNumericSlotCooccurrenceEvidence.StateInputStatusValidRows}; role_input_status_valid_rows={originalStateNumericSlotCooccurrenceEvidence.RoleInputStatusValidRows}; primitive_groups={originalStateNumericSlotCooccurrenceEvidence.PrimitiveGroups}; groups_with_five_role_rows={originalStateNumericSlotCooccurrenceEvidence.GroupsWithFiveRoleRows}; complete_five_slot_groups={originalStateNumericSlotCooccurrenceEvidence.CompleteFiveSlotGroups}; state_relocation_rows={originalStateNumericSlotCooccurrenceEvidence.StateRelocationRows}; shapename_state_rows={originalStateNumericSlotCooccurrenceEvidence.ShapenameStateRows}; primitive_type_state_rows={originalStateNumericSlotCooccurrenceEvidence.PrimitiveTypeStateRows}; exact_adjacency_stream_rows={originalStateNumericSlotCooccurrenceEvidence.ExactAdjacencyStreamRows}; complete_groups_with_shapename_state={originalStateNumericSlotCooccurrenceEvidence.CompleteGroupsWithShapenameState}; complete_groups_without_shapename_state={originalStateNumericSlotCooccurrenceEvidence.CompleteGroupsWithoutShapenameState}; complete_groups_with_primitive_type_state={originalStateNumericSlotCooccurrenceEvidence.CompleteGroupsWithPrimitiveTypeState}; complete_groups_without_primitive_type_state={originalStateNumericSlotCooccurrenceEvidence.CompleteGroupsWithoutPrimitiveTypeState}; complete_groups_with_unique_near_unit_float2={originalStateNumericSlotCooccurrenceEvidence.CompleteGroupsWithUniqueNearUnitFloat2}; complete_groups_with_slot1_near_unit_float2={originalStateNumericSlotCooccurrenceEvidence.CompleteGroupsWithSlot1NearUnitFloat2}; complete_groups_with_other_slot_near_unit_float2={originalStateNumericSlotCooccurrenceEvidence.CompleteGroupsWithOtherSlotNearUnitFloat2}; incomplete_five_slot_groups={originalStateNumericSlotCooccurrenceEvidence.IncompleteFiveSlotGroups}; role_row_count_histogram={originalStateNumericSlotCooccurrenceEvidence.RoleRowCountHistogram}; semantic_binding={originalStateNumericSlotCooccurrenceEvidence.SemanticBinding}; status={originalStateNumericSlotCooccurrenceEvidence.Status}; provenance={originalStateNumericSlotCooccurrenceEvidence.Provenance}; original_semantics={originalStateNumericSlotCooccurrenceEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-command-slot-role-cooccurrence-control",
                  originalCommandSlotRoleCooccurrenceEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalCommandSlotRoleCooccurrenceEvidence.Valid,
                  originalCommandSlotRoleCooccurrenceEvidence.CompleteGroupsWithSlot1NearUnitFloat2,
                  Math.Max(1, originalCommandSlotRoleCooccurrenceEvidence.CompleteFiveSlotGroups),
                  "independent primitive-key join reconciles uniquely identified current command ordinals with the numeric slot-role groups while retaining the two incomplete groups",
                  "does not call a slot UV, position, material, attribute or draw input and does not establish state-to-draw, skinning, rigging, animation, morph or GPU semantics",
                  $"descriptor_rows={originalCommandSlotRoleCooccurrenceEvidence.DescriptorRows}; role_rows={originalCommandSlotRoleCooccurrenceEvidence.RoleRows}; joined_rows={originalCommandSlotRoleCooccurrenceEvidence.JoinedRows}; missing_descriptor_keys={originalCommandSlotRoleCooccurrenceEvidence.MissingDescriptorKeys}; missing_role_keys={originalCommandSlotRoleCooccurrenceEvidence.MissingRoleKeys}; duplicate_descriptor_keys={originalCommandSlotRoleCooccurrenceEvidence.DuplicateDescriptorKeys}; duplicate_role_keys={originalCommandSlotRoleCooccurrenceEvidence.DuplicateRoleKeys}; descriptor_input_status_valid_rows={originalCommandSlotRoleCooccurrenceEvidence.DescriptorInputStatusValidRows}; role_input_status_valid_rows={originalCommandSlotRoleCooccurrenceEvidence.RoleInputStatusValidRows}; current_ordinal_present_rows={originalCommandSlotRoleCooccurrenceEvidence.CurrentOrdinalPresentRows}; current_ordinal_identity_match_rows={originalCommandSlotRoleCooccurrenceEvidence.CurrentOrdinalIdentityMatchRows}; current_ordinal_uniquely_identified_rows={originalCommandSlotRoleCooccurrenceEvidence.CurrentOrdinalUniquelyIdentifiedRows}; shifted_ordinal_identity_match_rows={originalCommandSlotRoleCooccurrenceEvidence.ShiftedOrdinalIdentityMatchRows}; primitive_groups={originalCommandSlotRoleCooccurrenceEvidence.PrimitiveGroups}; groups_with_five_role_rows={originalCommandSlotRoleCooccurrenceEvidence.GroupsWithFiveRoleRows}; complete_five_slot_groups={originalCommandSlotRoleCooccurrenceEvidence.CompleteFiveSlotGroups}; complete_groups_with_unique_near_unit_float2={originalCommandSlotRoleCooccurrenceEvidence.CompleteGroupsWithUniqueNearUnitFloat2}; complete_groups_with_slot1_near_unit_float2={originalCommandSlotRoleCooccurrenceEvidence.CompleteGroupsWithSlot1NearUnitFloat2}; complete_groups_with_other_slot_near_unit_float2={originalCommandSlotRoleCooccurrenceEvidence.CompleteGroupsWithOtherSlotNearUnitFloat2}; incomplete_five_slot_groups={originalCommandSlotRoleCooccurrenceEvidence.IncompleteFiveSlotGroups}; role_row_count_histogram={originalCommandSlotRoleCooccurrenceEvidence.RoleRowCountHistogram}; semantic_binding={originalCommandSlotRoleCooccurrenceEvidence.SemanticBinding}; status={originalCommandSlotRoleCooccurrenceEvidence.Status}; provenance={originalCommandSlotRoleCooccurrenceEvidence.Provenance}; original_semantics={originalCommandSlotRoleCooccurrenceEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-geometry-bounds-control",
                  originalRenderStateGeometryBoundsControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalRenderStateGeometryBoundsControlEvidence.Valid,
                  originalRenderStateGeometryBoundsControlEvidence.CompleteControlRows,
                  Math.Max(1, originalRenderStateGeometryBoundsControlEvidence.PartitionObjectRows),
                  "independent object-key synthesis joins state/geometry interval partition with bounds-consistent first raw fetch-slot observations for the candidate object set",
                  "does not establish state-to-draw, state-to-geometry, position, UV, material, skinning, rigging, animation, morph or GPU semantics",
                  $"partition_object_rows={originalRenderStateGeometryBoundsControlEvidence.PartitionObjectRows}; bounds_object_rows={originalRenderStateGeometryBoundsControlEvidence.BoundsObjectRows}; joined_object_rows={originalRenderStateGeometryBoundsControlEvidence.JoinedObjectRows}; partition_input_status_valid_rows={originalRenderStateGeometryBoundsControlEvidence.PartitionInputStatusValidRows}; bounds_input_status_valid_rows={originalRenderStateGeometryBoundsControlEvidence.BoundsInputStatusValidRows}; object_cardinality_match_rows={originalRenderStateGeometryBoundsControlEvidence.ObjectCardinalityMatchRows}; bounds_group_rows={originalRenderStateGeometryBoundsControlEvidence.BoundsGroupRows}; complete_fetch_group_rows={originalRenderStateGeometryBoundsControlEvidence.CompleteFetchGroupRows}; expected_incomplete_fetch_group_rows={originalRenderStateGeometryBoundsControlEvidence.ExpectedIncompleteFetchGroupRows}; bounds_slot0_match_rows={originalRenderStateGeometryBoundsControlEvidence.BoundsSlot0MatchRows}; bounds_nonzero_match_rows={originalRenderStateGeometryBoundsControlEvidence.BoundsNonzeroMatchRows}; exact_command_shape_rows={originalRenderStateGeometryBoundsControlEvidence.ExactCommandShapeRows}; complete_control_rows={originalRenderStateGeometryBoundsControlEvidence.CompleteControlRows}; semantic_binding={originalRenderStateGeometryBoundsControlEvidence.SemanticBinding}; status={originalRenderStateGeometryBoundsControlEvidence.Status}; provenance={originalRenderStateGeometryBoundsControlEvidence.Provenance}; original_semantics={originalRenderStateGeometryBoundsControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-population-negative-control",
                  string.Equals(originalRenderChainEvidence.StateFetchControlStatus, EaglOriginalRenderChainAudit.StateFetchControlStatus, StringComparison.Ordinal) ? "verified-structural-negative" : "not-present",
                  string.Equals(originalRenderChainEvidence.StateFetchControlStatus, EaglOriginalRenderChainAudit.StateFetchControlStatus, StringComparison.Ordinal),
                  originalRenderChainEvidence.PrimitiveRows - originalRenderChainEvidence.StateShapePrimitiveTypeOverlapRows,
                  Math.Max(1, originalRenderChainEvidence.PrimitiveRows),
                  "raw same-trunk state-population control: SHAPENAME and SetPrimitiveType assignment rows remain disjoint while fetch-slot counts are retained as opaque structure",
                  "does not prove state-to-geometry, UV, material, skinning, rigging, animation, morph or GPU binding; state/fetch co-occurrence remains observational",
                  $"primitive_rows={originalRenderChainEvidence.PrimitiveRows}; fetch_slot_rows={originalRenderChainEvidence.FetchSlotRows}; complete_five_slot_fetch_rows={originalRenderChainEvidence.CompleteFiveSlotFetchRows}; state_fetch_overlap_rows={originalRenderChainEvidence.StateFetchOverlapRows}; state_shapename_primitive_type_overlap_rows={originalRenderChainEvidence.StateShapePrimitiveTypeOverlapRows}; status={originalRenderChainEvidence.StateFetchControlStatus}; provenance={originalRenderChainEvidence.Provenance}; original_semantics={originalRenderChainEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-fetch-adjacency-control",
                  originalStateFetchAdjacencyControlEvidence.Valid ? "verified-structural" : "not-present",
                  originalStateFetchAdjacencyControlEvidence.Valid,
                  originalStateFetchAdjacencyControlEvidence.JoinedStateFetchExactAdjacencyRows,
                  Math.Max(1, originalStateFetchAdjacencyControlEvidence.JoinedRows),
                  "independent primitive-key reparse joins raw state/fetch population to the exact command/__VB adjacency rows",
                  "does not prove state-to-geometry, UV, material, skinning, rigging, animation, morph or GPU binding; co-occurrence remains observational",
                  $"render_rows={originalStateFetchAdjacencyControlEvidence.RenderRows}; adjacency_rows={originalStateFetchAdjacencyControlEvidence.AdjacencyRows}; joined_rows={originalStateFetchAdjacencyControlEvidence.JoinedRows}; state_relocation_rows={originalStateFetchAdjacencyControlEvidence.StateRelocationRows}; shapename_rows={originalStateFetchAdjacencyControlEvidence.ShapeNameRows}; primitive_type_rows={originalStateFetchAdjacencyControlEvidence.PrimitiveTypeRows}; complete_five_slot_fetch_rows={originalStateFetchAdjacencyControlEvidence.CompleteFiveSlotFetchRows}; reported_state_fetch_overlap_rows={originalStateFetchAdjacencyControlEvidence.ReportedStateFetchOverlapRows}; recomputed_state_fetch_overlap_rows={originalStateFetchAdjacencyControlEvidence.RecomputedStateFetchOverlapRows}; joined_exact_adjacency_rows={originalStateFetchAdjacencyControlEvidence.JoinedExactAdjacencyRows}; joined_state_exact_adjacency_rows={originalStateFetchAdjacencyControlEvidence.JoinedStateExactAdjacencyRows}; joined_state_fetch_exact_adjacency_rows={originalStateFetchAdjacencyControlEvidence.JoinedStateFetchExactAdjacencyRows}; exact_adjacency_category_histogram={originalStateFetchAdjacencyControlEvidence.ExactAdjacencyCategoryHistogram}; state_exact_adjacency_category_histogram={originalStateFetchAdjacencyControlEvidence.StateExactAdjacencyCategoryHistogram}; status={originalStateFetchAdjacencyControlEvidence.Status}; provenance={originalStateFetchAdjacencyControlEvidence.Provenance}; original_semantics={originalStateFetchAdjacencyControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-primitive-fetch-topology-control",
                  originalPrimitiveFetchTopologyControlEvidence.Valid ? "verified-structural" : "not-present",
                  originalPrimitiveFetchTopologyControlEvidence.Valid,
                  originalPrimitiveFetchTopologyControlEvidence.FetchSlotIdSetMatchRows,
                  Math.Max(1, originalPrimitiveFetchTopologyControlEvidence.JoinedRows),
                  "independent primitive-key reparse matches render fetch-slot IDs to five-row original topology groups and preserves incomplete slot cases",
                  "does not assign slot or stream fields to position, geometry, UV, material, skinning, rigging, animation, morph, command or GPU semantics",
                  $"render_rows={originalPrimitiveFetchTopologyControlEvidence.RenderRows}; topology_rows={originalPrimitiveFetchTopologyControlEvidence.TopologyRows}; joined_rows={originalPrimitiveFetchTopologyControlEvidence.JoinedRows}; render_input_status_valid_rows={originalPrimitiveFetchTopologyControlEvidence.RenderInputStatusValidRows}; topology_input_status_valid_rows={originalPrimitiveFetchTopologyControlEvidence.TopologyInputStatusValidRows}; topology_groups_with_five_rows={originalPrimitiveFetchTopologyControlEvidence.TopologyGroupsWithFiveRows}; topology_valid_slot_rows={originalPrimitiveFetchTopologyControlEvidence.TopologyValidSlotRows}; topology_key_ordinal_match_rows={originalPrimitiveFetchTopologyControlEvidence.TopologyKeyOrdinalMatchRows}; topology_valid_payload_range_rows={originalPrimitiveFetchTopologyControlEvidence.TopologyValidPayloadRangeRows}; topology_structural_match_rows={originalPrimitiveFetchTopologyControlEvidence.TopologyStructuralMatchRows}; render_fetch_slot_rows={originalPrimitiveFetchTopologyControlEvidence.RenderFetchSlotRows}; render_complete_five_slot_rows={originalPrimitiveFetchTopologyControlEvidence.RenderCompleteFiveSlotRows}; topology_complete_five_slot_rows={originalPrimitiveFetchTopologyControlEvidence.TopologyCompleteFiveSlotRows}; fetch_slot_count_match_rows={originalPrimitiveFetchTopologyControlEvidence.FetchSlotCountMatchRows}; fetch_slot_id_set_match_rows={originalPrimitiveFetchTopologyControlEvidence.FetchSlotIdSetMatchRows}; complete_five_slot_sequence_agreement_rows={originalPrimitiveFetchTopologyControlEvidence.CompleteFiveSlotSequenceAgreementRows}; incomplete_slot_two_rows={originalPrimitiveFetchTopologyControlEvidence.IncompleteSlotTwoRows}; missing_render_keys={originalPrimitiveFetchTopologyControlEvidence.MissingRenderKeys}; missing_topology_keys={originalPrimitiveFetchTopologyControlEvidence.MissingTopologyKeys}; status={originalPrimitiveFetchTopologyControlEvidence.Status}; provenance={originalPrimitiveFetchTopologyControlEvidence.Provenance}; original_semantics={originalPrimitiveFetchTopologyControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-command-payload-region-control",
                  originalCommandPayloadRegionControlEvidence.Valid ? "verified-structural" : "not-present",
                  originalCommandPayloadRegionControlEvidence.Valid,
                  originalCommandPayloadRegionControlEvidence.CommandPayloadNonoverlapRows,
                  Math.Max(1, originalCommandPayloadRegionControlEvidence.JoinedRows),
                  "independent primitive-key reparse checks half-open command/payload intervals, section equality, non-overlap and the raw command-to-payload gap histogram",
                  "does not assign payload or command bytes to position, geometry, UV, material, skinning, rigging, animation, morph, draw or GPU semantics",
                  $"render_rows={originalCommandPayloadRegionControlEvidence.RenderRows}; topology_rows={originalCommandPayloadRegionControlEvidence.TopologyRows}; joined_rows={originalCommandPayloadRegionControlEvidence.JoinedRows}; render_input_status_valid_rows={originalCommandPayloadRegionControlEvidence.RenderInputStatusValidRows}; topology_input_status_valid_rows={originalCommandPayloadRegionControlEvidence.TopologyInputStatusValidRows}; topology_groups_with_five_rows={originalCommandPayloadRegionControlEvidence.TopologyGroupsWithFiveRows}; payload_range_valid_rows={originalCommandPayloadRegionControlEvidence.PayloadRangeValidRows}; payload_interval_nonoverlap_rows={originalCommandPayloadRegionControlEvidence.PayloadIntervalNonoverlapRows}; command_section_match_rows={originalCommandPayloadRegionControlEvidence.CommandSectionMatchRows}; command_interval_valid_rows={originalCommandPayloadRegionControlEvidence.CommandIntervalValidRows}; command_payload_nonoverlap_rows={originalCommandPayloadRegionControlEvidence.CommandPayloadNonoverlapRows}; command_after_payload_end_rows={originalCommandPayloadRegionControlEvidence.CommandAfterPayloadEndRows}; command_payload_gap_equals_0x10_rows={originalCommandPayloadRegionControlEvidence.CommandPayloadGapEquals0x10Rows}; command_payload_gap_histogram={originalCommandPayloadRegionControlEvidence.CommandPayloadGapHistogram}; incomplete_slot_two_rows={originalCommandPayloadRegionControlEvidence.IncompleteSlotTwoRows}; missing_render_keys={originalCommandPayloadRegionControlEvidence.MissingRenderKeys}; missing_topology_keys={originalCommandPayloadRegionControlEvidence.MissingTopologyKeys}; status={originalCommandPayloadRegionControlEvidence.Status}; provenance={originalCommandPayloadRegionControlEvidence.Provenance}; original_semantics={originalCommandPayloadRegionControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-population-control",
                  originalStatePopulationControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStatePopulationControlEvidence.Valid,
                  originalStatePopulationControlEvidence.PrimitiveRows,
                  Math.Max(1, originalStatePopulationControlEvidence.PrimitiveRows),
                  "raw same-trunk population comparison of observed SHAPENAME and SetPrimitiveType assignment rows by object, index, layer, command hash and boundary shape",
                  "does not prove state-to-geometry, UV, material, skinning, rigging, animation, morph or GPU binding; repeated pairs remain opaque structural observations",
                  $"object_rows={originalStatePopulationControlEvidence.ObjectRows}; objects_with_rows={originalStatePopulationControlEvidence.ObjectsWithRows}; primitive_rows={originalStatePopulationControlEvidence.PrimitiveRows}; shapename_rows={originalStatePopulationControlEvidence.ShapeNameRows}; primitive_type_rows={originalStatePopulationControlEvidence.PrimitiveTypeRows}; population_overlap_rows={originalStatePopulationControlEvidence.PopulationOverlapRows}; same_primitive_index_pairs={originalStatePopulationControlEvidence.SamePrimitiveIndexPairs}; adjacent_primitive_pairs={originalStatePopulationControlEvidence.AdjacentPrimitivePairs}; same_layer_pairs={originalStatePopulationControlEvidence.SameLayerPairs}; exact_command_hash_pairs={originalStatePopulationControlEvidence.ExactCommandHashPairs}; exact_boundary_shape_pairs={originalStatePopulationControlEvidence.ExactBoundaryShapePairs}; complete_shapename_rows={originalStatePopulationControlEvidence.CompleteShapeNameRows}; complete_primitive_type_rows={originalStatePopulationControlEvidence.CompletePrimitiveTypeRows}; status={originalStatePopulationControlEvidence.Status}; provenance={originalStatePopulationControlEvidence.Provenance}; original_semantics={originalStatePopulationControlEvidence.OriginalSemantics}"),
            new(
                  "rendering-original-state-token-negative-control",
                  originalStateTokenEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateTokenEvidence.Valid,
                  originalStateTokenEvidence.ValidInputRows,
                  Math.Max(1, originalStateTokenEvidence.InputRows),
                  "literal assignment-name inventory from raw original EAGL state symbols; explicit texture/material/UV-coordinate/vertex-attribute/deformation token classes remain empty",
                  "does not prove semantic absence from opaque binary commands and does not assign SHAPENAME, clamps or primitive state to geometry, UV, material, skinning, rigging, animation, morph or GPU behavior",
                  $"input_rows={originalStateTokenEvidence.InputRows}; state_rows={originalStateTokenEvidence.StateRows}; valid_input_rows={originalStateTokenEvidence.ValidInputRows}; shapename_rows={originalStateTokenEvidence.ShapeNameRows}; clamp_u_rows={originalStateTokenEvidence.ClampURows}; clamp_v_rows={originalStateTokenEvidence.ClampVRows}; primitive_type_rows={originalStateTokenEvidence.PrimitiveTypeRows}; texture_token_rows={originalStateTokenEvidence.TextureTokenRows}; material_token_rows={originalStateTokenEvidence.MaterialTokenRows}; uv_coordinate_token_rows={originalStateTokenEvidence.UvCoordinateTokenRows}; vertex_attribute_token_rows={originalStateTokenEvidence.VertexAttributeTokenRows}; deformation_token_rows={originalStateTokenEvidence.DeformationTokenRows}; assignment_name_catalog={originalStateTokenEvidence.AssignmentNameCatalog}; status={originalStateTokenEvidence.Status}; provenance={originalStateTokenEvidence.Provenance}; original_semantics={originalStateTokenEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-candidate-attribute-control",
                  originalStateCandidateAttributeControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateCandidateAttributeControlEvidence.Valid,
                  originalStateCandidateAttributeControlEvidence.JoinedObjectRows,
                  Math.Max(1, originalStateCandidateAttributeControlEvidence.CandidateObjectRows),
                  "independent object-key join reconciles 24 candidate attribute/raster objects with their original state-token rows; slot-1 profiles and raster influence remain candidate-only observations",
                  "does not prove state-to-geometry, UV, material, skinning, rigging, animation, morph, command or GPU binding; no candidate slot-1 or raster observation is promoted to original semantics",
                  $"candidate_object_rows={originalStateCandidateAttributeControlEvidence.CandidateObjectRows}; state_object_rows={originalStateCandidateAttributeControlEvidence.StateObjectRows}; influence_object_rows={originalStateCandidateAttributeControlEvidence.InfluenceObjectRows}; joined_object_rows={originalStateCandidateAttributeControlEvidence.JoinedObjectRows}; missing_state_objects={originalStateCandidateAttributeControlEvidence.MissingStateObjects}; missing_attribute_objects={originalStateCandidateAttributeControlEvidence.MissingAttributeObjects}; missing_influence_objects={originalStateCandidateAttributeControlEvidence.MissingInfluenceObjects}; state_rows_joined={originalStateCandidateAttributeControlEvidence.StateRowsJoined}; state_valid_rows={originalStateCandidateAttributeControlEvidence.StateValidRows}; state_negative_token_rows={originalStateCandidateAttributeControlEvidence.StateNegativeTokenRows}; attribute_complete_rows={originalStateCandidateAttributeControlEvidence.AttributeCompleteRows}; attribute_finite_rows={originalStateCandidateAttributeControlEvidence.AttributeFiniteRows}; attribute_near_unit_rows={originalStateCandidateAttributeControlEvidence.AttributeNearUnitRows}; influence_geometry_rows={originalStateCandidateAttributeControlEvidence.InfluenceGeometryRows}; influence_status_rows={originalStateCandidateAttributeControlEvidence.InfluenceStatusRows}; influence_texture_rows={originalStateCandidateAttributeControlEvidence.InfluenceTextureRows}; influence_mesh_shape_state_exact_rows={originalStateCandidateAttributeControlEvidence.InfluenceMeshShapeStateExactRows}; status={originalStateCandidateAttributeControlEvidence.Status}; provenance={originalStateCandidateAttributeControlEvidence.Provenance}; original_semantics={originalStateCandidateAttributeControlEvidence.OriginalSemantics}"),
              new(
                  "assets-candidate-attribute-texture-compatibility-control",
                  candidateAttributeTextureCompatibilityEvidence.Valid ? "verified-structural-negative" : "not-present",
                  candidateAttributeTextureCompatibilityEvidence.Valid,
                  candidateAttributeTextureCompatibilityEvidence.AttributeTextureCompatibilityRows,
                  Math.Max(1, candidateAttributeTextureCompatibilityEvidence.AttributeObjectRows),
                  "independent candidate object-key join checks opaque attribute ranges against same-trunk texture dimensions, PAL8 metadata and decoded pixels",
                  "does not identify slot 1 as UV, does not assign texture sampling or material semantics, and does not establish any original state, draw or GPU binding",
                  $"attribute_object_rows={candidateAttributeTextureCompatibilityEvidence.AttributeObjectRows}; asset_object_rows={candidateAttributeTextureCompatibilityEvidence.AssetObjectRows}; joined_object_rows={candidateAttributeTextureCompatibilityEvidence.JoinedObjectRows}; attribute_input_status_valid_rows={candidateAttributeTextureCompatibilityEvidence.AttributeInputStatusValidRows}; asset_input_status_valid_rows={candidateAttributeTextureCompatibilityEvidence.AssetInputStatusValidRows}; texture_dimensions_valid_rows={candidateAttributeTextureCompatibilityEvidence.TextureDimensionsValidRows}; texture_pal8_rows={candidateAttributeTextureCompatibilityEvidence.TexturePal8Rows}; decoded_pixel_match_rows={candidateAttributeTextureCompatibilityEvidence.DecodedPixelMatchRows}; slot1_range_rows={candidateAttributeTextureCompatibilityEvidence.Slot1RangeRows}; slot1_finite_range_rows={candidateAttributeTextureCompatibilityEvidence.Slot1FiniteRangeRows}; slot1_normalized_range_rows={candidateAttributeTextureCompatibilityEvidence.Slot1NormalizedRangeRows}; slot1_value_count_match_vertex_rows={candidateAttributeTextureCompatibilityEvidence.Slot1ValueCountMatchVertexRows}; attribute_texture_compatibility_rows={candidateAttributeTextureCompatibilityEvidence.AttributeTextureCompatibilityRows}; semantic_binding={candidateAttributeTextureCompatibilityEvidence.SemanticBinding}; status={candidateAttributeTextureCompatibilityEvidence.Status}; provenance={candidateAttributeTextureCompatibilityEvidence.Provenance}; original_semantics={candidateAttributeTextureCompatibilityEvidence.OriginalSemantics}"),
              new(
                  "assets-candidate-attribute-raster-influence",
                  candidateAttributeRasterInfluenceVerified ? "verified-candidate" : "not-present",
                  candidateAttributeRasterInfluenceVerified,
                  candidateAttributeRasterInfluenceVerifiedRows,
                  Math.Max(1, candidateAttributeRasterInfluenceCandidateRows.Length),
                  "candidate-only baseline versus deterministic opaque-attribute perturbation changes the managed textured raster for every same-trunk object",
                  "does not identify slot 1 as UV, does not assign original material/sampling semantics, and does not establish original GPU or state binding",
                  $"candidate_rows={candidateAttributeRasterInfluenceCandidateRows.Length}; verified_rows={candidateAttributeRasterInfluenceVerifiedRows}; geometry_finite_and_indexed_rows={ReadInt(candidateAttributeRasterInfluenceSummary, "geometry_finite_and_indexed")}; attribute_cardinality_valid_rows={ReadInt(candidateAttributeRasterInfluenceSummary, "attribute_cardinality_valid")}; attribute_perturbs_raster_rows={ReadInt(candidateAttributeRasterInfluenceSummary, "attribute_perturbs_raster")}; distinct_baseline_checksums={ReadInt(candidateAttributeRasterInfluenceSummary, "baseline_checksum")}; distinct_perturbed_checksums={ReadInt(candidateAttributeRasterInfluenceSummary, "perturbed_checksum")}; status={Get(candidateAttributeRasterInfluenceSummary, "status")}; original_semantics={Get(candidateAttributeRasterInfluenceSummary, "original_semantics")}"),
              new(
                  "rendering-original-shapename-candidate-asset-control",
                  originalShapenameCandidateAssetControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalShapenameCandidateAssetControlEvidence.Valid,
                  originalShapenameCandidateAssetControlEvidence.JoinedRows,
                  Math.Max(1, originalShapenameCandidateAssetControlEvidence.CandidateRows),
                  "independent object-key join lexically matches original SHAPENAME tokens to same-trunk candidate shape/source/pixel rows",
                  "does not turn SHAPENAME into a material, UV, vertex, skinning, rigging, animation, morph, command or GPU binding",
                  $"state_rows={originalShapenameCandidateAssetControlEvidence.StateRows}; candidate_rows={originalShapenameCandidateAssetControlEvidence.CandidateRows}; joined_rows={originalShapenameCandidateAssetControlEvidence.JoinedRows}; missing_candidate_objects={originalShapenameCandidateAssetControlEvidence.MissingCandidateObjects}; missing_state_objects={originalShapenameCandidateAssetControlEvidence.MissingStateObjects}; state_input_valid_rows={originalShapenameCandidateAssetControlEvidence.StateInputValidRows}; candidate_input_valid_rows={originalShapenameCandidateAssetControlEvidence.CandidateInputValidRows}; object_state_exact_rows={originalShapenameCandidateAssetControlEvidence.ObjectStateExactRows}; shapename_token_match_rows={originalShapenameCandidateAssetControlEvidence.ShapenameTokenMatchRows}; same_trunk_rows={originalShapenameCandidateAssetControlEvidence.SameTrunkRows}; source_chain_valid_rows={originalShapenameCandidateAssetControlEvidence.SourceChainValidRows}; shape_entry_exact_rows={originalShapenameCandidateAssetControlEvidence.ShapeEntryExactRows}; decoded_pixel_match_rows={originalShapenameCandidateAssetControlEvidence.DecodedPixelMatchRows}; status={originalShapenameCandidateAssetControlEvidence.Status}; provenance={originalShapenameCandidateAssetControlEvidence.Provenance}; original_semantics={originalShapenameCandidateAssetControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-candidate-primitive-shape-control",
                  originalStateCandidatePrimitiveShapeControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateCandidatePrimitiveShapeControlEvidence.Valid,
                  originalStateCandidatePrimitiveShapeControlEvidence.JoinedRows,
                  Math.Max(1, originalStateCandidatePrimitiveShapeControlEvidence.CandidateRows),
                  "independent object-key join reconciles original SHAPENAME primitive population with candidate shape-state stream population",
                  "does not turn matching primitive populations into primitive-to-vertex, UV, material, skinning, rigging, animation, morph, command or GPU bindings",
                  $"state_rows={originalStateCandidatePrimitiveShapeControlEvidence.StateRows}; candidate_rows={originalStateCandidatePrimitiveShapeControlEvidence.CandidateRows}; joined_rows={originalStateCandidatePrimitiveShapeControlEvidence.JoinedRows}; missing_candidate_objects={originalStateCandidatePrimitiveShapeControlEvidence.MissingCandidateObjects}; missing_state_objects={originalStateCandidatePrimitiveShapeControlEvidence.MissingStateObjects}; state_input_valid_rows={originalStateCandidatePrimitiveShapeControlEvidence.StateInputValidRows}; candidate_input_valid_rows={originalStateCandidatePrimitiveShapeControlEvidence.CandidateInputValidRows}; state_primitive_count_match_rows={originalStateCandidatePrimitiveShapeControlEvidence.StatePrimitiveCountMatchRows}; shapename_count_match_rows={originalStateCandidatePrimitiveShapeControlEvidence.ShapenameCountMatchRows}; candidate_shape_state_streams_complete_rows={originalStateCandidatePrimitiveShapeControlEvidence.CandidateShapeStateStreamsCompleteRows}; same_trunk_rows={originalStateCandidatePrimitiveShapeControlEvidence.SameTrunkRows}; source_chain_valid_rows={originalStateCandidatePrimitiveShapeControlEvidence.SourceChainValidRows}; shape_entry_exact_rows={originalStateCandidatePrimitiveShapeControlEvidence.ShapeEntryExactRows}; decoded_pixel_match_rows={originalStateCandidatePrimitiveShapeControlEvidence.DecodedPixelMatchRows}; status={originalStateCandidatePrimitiveShapeControlEvidence.Status}; provenance={originalStateCandidatePrimitiveShapeControlEvidence.Provenance}; original_semantics={originalStateCandidatePrimitiveShapeControlEvidence.OriginalSemantics}"),
               new(
                   "rendering-original-candidate-shapename-primitive-index-control",
                   originalCandidateShapenamePrimitiveIndexControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                   originalCandidateShapenamePrimitiveIndexControlEvidence.Valid,
                   originalCandidateShapenamePrimitiveIndexControlEvidence.JoinedRows,
                   Math.Max(1, originalCandidateShapenamePrimitiveIndexControlEvidence.CandidateRows),
                   "independent object-key join finds exact original/candidate SHAPENAME layer:primitive sequence, set and count agreement",
                   "does not turn index/order agreement into primitive-to-vertex, UV, material, skinning, rigging, animation, morph, command or GPU binding",
                   $"state_rows={originalCandidateShapenamePrimitiveIndexControlEvidence.StateRows}; candidate_rows={originalCandidateShapenamePrimitiveIndexControlEvidence.CandidateRows}; joined_rows={originalCandidateShapenamePrimitiveIndexControlEvidence.JoinedRows}; missing_candidate_objects={originalCandidateShapenamePrimitiveIndexControlEvidence.MissingCandidateObjects}; missing_state_objects={originalCandidateShapenamePrimitiveIndexControlEvidence.MissingStateObjects}; sequence_match_rows={originalCandidateShapenamePrimitiveIndexControlEvidence.SequenceMatchRows}; set_match_rows={originalCandidateShapenamePrimitiveIndexControlEvidence.SetMatchRows}; count_match_rows={originalCandidateShapenamePrimitiveIndexControlEvidence.CountMatchRows}; complete_candidate_rows={originalCandidateShapenamePrimitiveIndexControlEvidence.CompleteCandidateRows}; original_input_valid_rows={originalCandidateShapenamePrimitiveIndexControlEvidence.OriginalInputValidRows}; candidate_input_valid_rows={originalCandidateShapenamePrimitiveIndexControlEvidence.CandidateInputValidRows}; status={originalCandidateShapenamePrimitiveIndexControlEvidence.Status}; provenance={originalCandidateShapenamePrimitiveIndexControlEvidence.Provenance}; original_semantics={originalCandidateShapenamePrimitiveIndexControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-candidate-primitive-fetch-control",
                  originalCandidatePrimitiveFetchControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalCandidatePrimitiveFetchControlEvidence.Valid,
                  originalCandidatePrimitiveFetchControlEvidence.JoinedRows,
                  Math.Max(1, originalCandidatePrimitiveFetchControlEvidence.CandidateRows),
                  "independent primitive-key join reconciles original render-chain commands and fetch-slot topology with candidate fetch records; two known incomplete candidate rows remain explicitly unmaterialized",
                  "does not turn slot IDs, state text, primitive-type flags or command counts into position, UV, material, skinning, rigging, animation, morph, draw or GPU semantics",
                  $"original_rows={originalCandidatePrimitiveFetchControlEvidence.OriginalRows}; candidate_rows={originalCandidatePrimitiveFetchControlEvidence.CandidateRows}; joined_rows={originalCandidatePrimitiveFetchControlEvidence.JoinedRows}; missing_candidate_keys={originalCandidatePrimitiveFetchControlEvidence.MissingCandidateKeys}; missing_original_keys={originalCandidatePrimitiveFetchControlEvidence.MissingOriginalKeys}; command_count_match_rows={originalCandidatePrimitiveFetchControlEvidence.CommandCountMatchRows}; command_bytes_sha256_match_rows={originalCandidatePrimitiveFetchControlEvidence.CommandBytesSha256MatchRows}; command_stream_complete_match_rows={originalCandidatePrimitiveFetchControlEvidence.CommandStreamCompleteMatchRows}; fetch_slot_count_match_rows={originalCandidatePrimitiveFetchControlEvidence.FetchSlotCountMatchRows}; fetch_slot_id_sequence_match_rows={originalCandidatePrimitiveFetchControlEvidence.FetchSlotIdSequenceMatchRows}; complete_five_slot_sequence_match_rows={originalCandidatePrimitiveFetchControlEvidence.CompleteFiveSlotSequenceMatchRows}; state_assignment_sequence_match_rows={originalCandidatePrimitiveFetchControlEvidence.StateAssignmentSequenceMatchRows}; primitive_type_flag_match_rows={originalCandidatePrimitiveFetchControlEvidence.PrimitiveTypeFlagMatchRows}; candidate_slot_records_valid_rows={originalCandidatePrimitiveFetchControlEvidence.CandidateSlotRecordsValidRows}; candidate_slot_records_unmaterialized_expected_rows={originalCandidatePrimitiveFetchControlEvidence.CandidateSlotRecordsUnmaterializedExpectedRows}; original_input_valid_rows={originalCandidatePrimitiveFetchControlEvidence.OriginalInputValidRows}; candidate_input_valid_rows={originalCandidatePrimitiveFetchControlEvidence.CandidateInputValidRows}; status={originalCandidatePrimitiveFetchControlEvidence.Status}; provenance={originalCandidatePrimitiveFetchControlEvidence.Provenance}; original_semantics={originalCandidatePrimitiveFetchControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-geometry-draw-classification",
                  originalGeometryDrawClassificationEvidence.Valid ? "verified-structural" : "not-present",
                  originalGeometryDrawClassificationEvidence.Valid,
                  originalGeometryDrawClassificationEvidence.ClassificationMatchRows,
                  Math.Max(1, originalGeometryDrawClassificationEvidence.JoinedRows),
                  "independent primitive-key join classifies original SetPrimitiveType/five-slot rows against candidate mesh groups and preserves SHAPENAME-only state rows as a separate structural class",
                  "does not associate state runs with a draw, or assign position, UV, material, skinning, rigging, animation, morph, command or GPU semantics",
                  $"candidate_object_rows={originalGeometryDrawClassificationEvidence.CandidateObjectRows}; original_scoped_rows={originalGeometryDrawClassificationEvidence.OriginalScopedRows}; candidate_rows={originalGeometryDrawClassificationEvidence.CandidateRows}; joined_rows={originalGeometryDrawClassificationEvidence.JoinedRows}; missing_original_keys={originalGeometryDrawClassificationEvidence.MissingOriginalKeys}; missing_candidate_keys={originalGeometryDrawClassificationEvidence.MissingCandidateKeys}; joined_object_rows={originalGeometryDrawClassificationEvidence.JoinedObjectRows}; objects_with_complete_classification={originalGeometryDrawClassificationEvidence.ObjectsWithCompleteClassification}; original_input_status_valid_rows={originalGeometryDrawClassificationEvidence.OriginalInputStatusValidRows}; candidate_input_status_valid_rows={originalGeometryDrawClassificationEvidence.CandidateInputStatusValidRows}; original_geometry_marker_rows={originalGeometryDrawClassificationEvidence.OriginalGeometryMarkerRows}; original_state_marker_rows={originalGeometryDrawClassificationEvidence.OriginalStateMarkerRows}; candidate_geometry_marker_rows={originalGeometryDrawClassificationEvidence.CandidateGeometryMarkerRows}; candidate_state_marker_rows={originalGeometryDrawClassificationEvidence.CandidateStateMarkerRows}; classification_match_rows={originalGeometryDrawClassificationEvidence.ClassificationMatchRows}; state_count_greater_than_geometry_rows={originalGeometryDrawClassificationEvidence.StateCountGreaterThanGeometryRows}; state_to_geometry_unique_binding_proven_rows={originalGeometryDrawClassificationEvidence.StateToGeometryUniqueBindingProvenRows}; status={originalGeometryDrawClassificationEvidence.Status}; semantic_binding={originalGeometryDrawClassificationEvidence.SemanticBinding}; provenance={originalGeometryDrawClassificationEvidence.Provenance}; original_semantics={originalGeometryDrawClassificationEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-candidate-material-ambiguity-control",
                  originalStateCandidateMaterialAmbiguityEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateCandidateMaterialAmbiguityEvidence.Valid,
                  originalStateCandidateMaterialAmbiguityEvidence.ObjectsWithCompleteAmbiguityControl,
                  Math.Max(1, originalStateCandidateMaterialAmbiguityEvidence.CandidateObjectRows),
                  "independent object join reconciles original state intervals, SHAPENAME candidate texture sources and geometry adjacency cardinality",
                  "does not assign an adjacent geometry to a state run or promote SHAPENAME/candidate texture to original material, UV, draw, command or GPU semantics",
                  $"candidate_object_rows={originalStateCandidateMaterialAmbiguityEvidence.CandidateObjectRows}; render_scoped_rows={originalStateCandidateMaterialAmbiguityEvidence.RenderScopedRows}; order_rows={originalStateCandidateMaterialAmbiguityEvidence.OrderRows}; order_state_rows={originalStateCandidateMaterialAmbiguityEvidence.OrderStateRows}; original_geometry_rows={originalStateCandidateMaterialAmbiguityEvidence.OriginalGeometryRows}; original_state_rows={originalStateCandidateMaterialAmbiguityEvidence.OriginalStateRows}; nonempty_state_interval_rows={originalStateCandidateMaterialAmbiguityEvidence.NonemptyStateIntervalRows}; intervals_with_two_adjacent_geometry_candidates={originalStateCandidateMaterialAmbiguityEvidence.IntervalsWithTwoAdjacentGeometryCandidates}; intervals_with_one_adjacent_geometry_candidate={originalStateCandidateMaterialAmbiguityEvidence.IntervalsWithOneAdjacentGeometryCandidate}; intervals_with_zero_adjacent_geometry_candidates={originalStateCandidateMaterialAmbiguityEvidence.IntervalsWithZeroAdjacentGeometryCandidates}; state_rows_joined={originalStateCandidateMaterialAmbiguityEvidence.StateRowsJoined}; state_rows_with_shape_token_match={originalStateCandidateMaterialAmbiguityEvidence.StateRowsWithShapeTokenMatch}; state_rows_with_candidate_asset_source={originalStateCandidateMaterialAmbiguityEvidence.StateRowsWithCandidateAssetSource}; candidate_asset_valid_rows={originalStateCandidateMaterialAmbiguityEvidence.CandidateAssetValidRows}; object_asset_join_rows={originalStateCandidateMaterialAmbiguityEvidence.ObjectAssetJoinRows}; objects_with_complete_ambiguity_control={originalStateCandidateMaterialAmbiguityEvidence.ObjectsWithCompleteAmbiguityControl}; original_input_status_valid_rows={originalStateCandidateMaterialAmbiguityEvidence.OriginalInputStatusValidRows}; order_input_status_valid_rows={originalStateCandidateMaterialAmbiguityEvidence.OrderInputStatusValidRows}; state_asset_input_status_valid_rows={originalStateCandidateMaterialAmbiguityEvidence.StateAssetInputStatusValidRows}; candidate_asset_input_status_valid_rows={originalStateCandidateMaterialAmbiguityEvidence.CandidateAssetInputStatusValidRows}; state_to_geometry_unique_binding_proven_rows={originalStateCandidateMaterialAmbiguityEvidence.StateToGeometryUniqueBindingProvenRows}; state_to_material_unique_binding_proven_rows={originalStateCandidateMaterialAmbiguityEvidence.StateToMaterialUniqueBindingProvenRows}; status={originalStateCandidateMaterialAmbiguityEvidence.Status}; semantic_binding={originalStateCandidateMaterialAmbiguityEvidence.SemanticBinding}; provenance={originalStateCandidateMaterialAmbiguityEvidence.Provenance}; original_semantics={originalStateCandidateMaterialAmbiguityEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-candidate-stream-record-control",
                  originalCandidateStreamRecordControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalCandidateStreamRecordControlEvidence.Valid,
                  originalCandidateStreamRecordControlEvidence.JoinedMaterializedSlotRows,
                  Math.Max(1, originalCandidateStreamRecordControlEvidence.CandidateSlotRowsMaterialized),
                  "independent primitive/slot join matches candidate stream names and raw __VB metadata to original topology for 1080 materialized slots; two incomplete candidate primitives remain explicit negatives",
                  "does not assign any stream or slot to position, UV, material, skinning, rigging, animation, morph, draw or GPU semantics",
                  $"original_rows={originalCandidateStreamRecordControlEvidence.OriginalRows}; original_primitive_rows={originalCandidateStreamRecordControlEvidence.OriginalPrimitiveRows}; candidate_rows={originalCandidateStreamRecordControlEvidence.CandidateRows}; joined_primitive_rows={originalCandidateStreamRecordControlEvidence.JoinedPrimitiveRows}; missing_candidate_keys={originalCandidateStreamRecordControlEvidence.MissingCandidateKeys}; missing_original_keys={originalCandidateStreamRecordControlEvidence.MissingOriginalKeys}; candidate_complete_primitive_rows={originalCandidateStreamRecordControlEvidence.CandidateCompletePrimitiveRows}; candidate_incomplete_primitive_rows={originalCandidateStreamRecordControlEvidence.CandidateIncompletePrimitiveRows}; candidate_slot_rows_materialized={originalCandidateStreamRecordControlEvidence.CandidateSlotRowsMaterialized}; joined_materialized_slot_rows={originalCandidateStreamRecordControlEvidence.JoinedMaterializedSlotRows}; symbol_exact_rows={originalCandidateStreamRecordControlEvidence.SymbolExactRows}; auxiliary_exact_rows={originalCandidateStreamRecordControlEvidence.AuxiliaryExactRows}; fetch_stride_exact_rows={originalCandidateStreamRecordControlEvidence.FetchStrideExactRows}; stream_stride_exact_rows={originalCandidateStreamRecordControlEvidence.StreamStrideExactRows}; byte_length_exact_rows={originalCandidateStreamRecordControlEvidence.ByteLengthExactRows}; element_count_exact_rows={originalCandidateStreamRecordControlEvidence.ElementCountExactRows}; record_exact_rows={originalCandidateStreamRecordControlEvidence.RecordExactRows}; candidate_slot_records_unmaterialized_expected_rows={originalCandidateStreamRecordControlEvidence.CandidateSlotRecordsUnmaterializedExpectedRows}; original_input_valid_rows={originalCandidateStreamRecordControlEvidence.OriginalInputValidRows}; candidate_input_valid_rows={originalCandidateStreamRecordControlEvidence.CandidateInputValidRows}; status={originalCandidateStreamRecordControlEvidence.Status}; provenance={originalCandidateStreamRecordControlEvidence.Provenance}; original_semantics={originalCandidateStreamRecordControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-candidate-slot-profile-control",
                  originalCandidateSlotProfileEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalCandidateSlotProfileEvidence.Valid,
                  originalCandidateSlotProfileEvidence.CompleteControlRows,
                  Math.Max(1, originalCandidateSlotProfileEvidence.CandidateCompleteFiveSlotRows),
                  "independent TSV reparse joins 216 complete candidate slot-1 records to same-object original category-11 numeric streams with exact symbol, stride, byte-length and element-count agreement; original float2 profiles are finite and near-unit",
                  "does not name slot 1 or category 11 as UV, material, normal, weight, skinning, rigging, animation, morph, command or GPU state; semantic binding remains unproven",
                  $"original_stream_rows={originalCandidateSlotProfileEvidence.OriginalStreamRows}; candidate_primitive_rows={originalCandidateSlotProfileEvidence.CandidatePrimitiveRows}; candidate_complete_five_slot_rows={originalCandidateSlotProfileEvidence.CandidateCompleteFiveSlotRows}; candidate_slot1_rows={originalCandidateSlotProfileEvidence.CandidateSlot1Rows}; symbol_match_rows={originalCandidateSlotProfileEvidence.SymbolMatchRows}; object_match_rows={originalCandidateSlotProfileEvidence.ObjectMatchRows}; category11_rows={originalCandidateSlotProfileEvidence.Category11Rows}; finite_float2_rows={originalCandidateSlotProfileEvidence.FiniteFloat2Rows}; near_unit_range_float2_rows={originalCandidateSlotProfileEvidence.NearUnitRangeFloat2Rows}; stream_stride_match_rows={originalCandidateSlotProfileEvidence.StreamStrideMatchRows}; byte_length_match_rows={originalCandidateSlotProfileEvidence.ByteLengthMatchRows}; element_count_match_rows={originalCandidateSlotProfileEvidence.ElementCountMatchRows}; complete_control_rows={originalCandidateSlotProfileEvidence.CompleteControlRows}; status={originalCandidateSlotProfileEvidence.Status}; provenance={originalCandidateSlotProfileEvidence.Provenance}; original_semantics={originalCandidateSlotProfileEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-candidate-geometry-stream-control",
                  originalCandidateGeometryStreamControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalCandidateGeometryStreamControlEvidence.Valid,
                  originalCandidateGeometryStreamControlEvidence.JoinedRows,
                  Math.Max(1, originalCandidateGeometryStreamControlEvidence.CandidateRows),
                  "independent primitive-key join matches the 38 candidate geometry groups to original slot-0 stream names, strides, byte lengths and strip cardinalities",
                  "does not promote the candidate source-slot choice or raw stream agreement to position, UV, material, skinning, rigging, animation, morph, draw or GPU semantics",
                  $"original_slot_zero_rows={originalCandidateGeometryStreamControlEvidence.OriginalSlotZeroRows}; candidate_rows={originalCandidateGeometryStreamControlEvidence.CandidateRows}; joined_rows={originalCandidateGeometryStreamControlEvidence.JoinedRows}; missing_candidate_keys={originalCandidateGeometryStreamControlEvidence.MissingCandidateKeys}; missing_original_keys={originalCandidateGeometryStreamControlEvidence.MissingOriginalKeys}; candidate_source_slot_zero_rows={originalCandidateGeometryStreamControlEvidence.CandidateSourceSlotZeroRows}; candidate_complete_rows={originalCandidateGeometryStreamControlEvidence.CandidateCompleteRows}; candidate_finite_and_indexed_rows={originalCandidateGeometryStreamControlEvidence.CandidateFiniteAndIndexedRows}; symbol_exact_rows={originalCandidateGeometryStreamControlEvidence.SymbolExactRows}; stream_stride_exact_rows={originalCandidateGeometryStreamControlEvidence.StreamStrideExactRows}; byte_length_exact_rows={originalCandidateGeometryStreamControlEvidence.ByteLengthExactRows}; element_count_exact_rows={originalCandidateGeometryStreamControlEvidence.ElementCountExactRows}; vertex_count_exact_rows={originalCandidateGeometryStreamControlEvidence.VertexCountExactRows}; triangle_count_exact_rows={originalCandidateGeometryStreamControlEvidence.TriangleCountExactRows}; original_input_valid_rows={originalCandidateGeometryStreamControlEvidence.OriginalInputValidRows}; candidate_input_valid_rows={originalCandidateGeometryStreamControlEvidence.CandidateInputValidRows}; status={originalCandidateGeometryStreamControlEvidence.Status}; provenance={originalCandidateGeometryStreamControlEvidence.Provenance}; original_semantics={originalCandidateGeometryStreamControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-candidate-geometry-payload-identity-control",
                  originalCandidateGeometryPayloadIdentityEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalCandidateGeometryPayloadIdentityEvidence.Valid,
                  originalCandidateGeometryPayloadIdentityEvidence.CompleteIdentityRows,
                  Math.Max(1, originalCandidateGeometryPayloadIdentityEvidence.GeometryRows),
                  "independent TSV reparse verifies all 38 geometry groups against original five-slot topology, candidate fetch metadata and candidate raw payload SHA-256 identities",
                  "does not promote byte identity to position, UV, material, skinning, rigging, animation, morph, draw or GPU semantics",
                  $"geometry_rows={originalCandidateGeometryPayloadIdentityEvidence.GeometryRows}; candidate_geometry_rows={originalCandidateGeometryPayloadIdentityEvidence.CandidateGeometryRows}; joined_rows={originalCandidateGeometryPayloadIdentityEvidence.JoinedRows}; missing_candidate_geometry_keys={originalCandidateGeometryPayloadIdentityEvidence.MissingCandidateGeometryKeys}; missing_candidate_fetch_keys={originalCandidateGeometryPayloadIdentityEvidence.MissingCandidateFetchKeys}; missing_raw_identity_keys={originalCandidateGeometryPayloadIdentityEvidence.MissingRawIdentityKeys}; original_slot_rows={originalCandidateGeometryPayloadIdentityEvidence.OriginalSlotRows}; candidate_slot_rows={originalCandidateGeometryPayloadIdentityEvidence.CandidateSlotRows}; raw_hash_rows={originalCandidateGeometryPayloadIdentityEvidence.RawHashRows}; symbol_exact_rows={originalCandidateGeometryPayloadIdentityEvidence.SymbolExactRows}; metadata_exact_rows={originalCandidateGeometryPayloadIdentityEvidence.MetadataExactRows}; payload_hash_exact_rows={originalCandidateGeometryPayloadIdentityEvidence.PayloadHashExactRows}; complete_identity_rows={originalCandidateGeometryPayloadIdentityEvidence.CompleteIdentityRows}; original_input_valid_rows={originalCandidateGeometryPayloadIdentityEvidence.OriginalInputValidRows}; candidate_input_valid_rows={originalCandidateGeometryPayloadIdentityEvidence.CandidateInputValidRows}; geometry_input_valid_rows={originalCandidateGeometryPayloadIdentityEvidence.GeometryInputValidRows}; raw_input_valid_rows={originalCandidateGeometryPayloadIdentityEvidence.RawInputValidRows}; status={originalCandidateGeometryPayloadIdentityEvidence.Status}; provenance={originalCandidateGeometryPayloadIdentityEvidence.Provenance}; original_semantics={originalCandidateGeometryPayloadIdentityEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-primitive-stream-topology",
                  originalPrimitiveStreamTopologyEvidence.Valid ? "verified-structural" : "not-present",
                  originalPrimitiveStreamTopologyEvidence.Valid,
                  originalPrimitiveStreamTopologyEvidence.CompletePrimitiveRows,
                  Math.Max(1, originalPrimitiveStreamTopologyEvidence.PrimitiveRows),
                  "raw original fetch-slot topology joined to validated __VB category/ordinal records and payload ranges; repeated ordinal layouts remain explicitly ambiguous",
                  "independent command/payload evidence that discriminates repeated ordinal layouts and establishes any attribute/geometry/UV/material semantics",
                  $"model_package_candidates={originalPrimitiveStreamTopologyEvidence.ModelPackageCandidates}; model_packages_loaded={originalPrimitiveStreamTopologyEvidence.ModelPackagesLoaded}; object_candidates={originalPrimitiveStreamTopologyEvidence.ObjectCandidates}; objects_loaded={originalPrimitiveStreamTopologyEvidence.ObjectsLoaded}; objects_with_validated_streams={originalPrimitiveStreamTopologyEvidence.ObjectsWithValidatedStreams}; primitive_rows={originalPrimitiveStreamTopologyEvidence.PrimitiveRows}; stream_rows={originalPrimitiveStreamTopologyEvidence.StreamRows}; primitive_rows_with_ordinal_matched_streams={originalPrimitiveStreamTopologyEvidence.PrimitiveRowsWithOrdinalMatchedStreams}; stream_rows_with_ordinal_match={originalPrimitiveStreamTopologyEvidence.StreamRowsWithOrdinalMatch}; stream_rows_with_stride_match={originalPrimitiveStreamTopologyEvidence.StreamRowsWithStrideMatch}; stream_rows_with_valid_payload_range={originalPrimitiveStreamTopologyEvidence.StreamRowsWithValidPayloadRange}; complete_primitive_rows={originalPrimitiveStreamTopologyEvidence.CompletePrimitiveRows}; objects_with_non_overlapping_payload_ranges={originalPrimitiveStreamTopologyEvidence.ObjectsWithNonOverlappingPayloadRanges}; reversed_slot_match_rows={originalPrimitiveStreamTopologyEvidence.ReversedSlotMatchRows}; shifted_ordinal_match_rows={originalPrimitiveStreamTopologyEvidence.ShiftedOrdinalMatchRows}; rejected_packages={originalPrimitiveStreamTopologyEvidence.RejectedPackages}; rejected_objects={originalPrimitiveStreamTopologyEvidence.RejectedObjects}; status={originalPrimitiveStreamTopologyEvidence.Status}; provenance={originalPrimitiveStreamTopologyEvidence.Provenance}; original_semantics={originalPrimitiveStreamTopologyEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-primitive-stream-opaque-discrimination",
                  originalPrimitiveStreamOpaqueDiscriminationEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalPrimitiveStreamOpaqueDiscriminationEvidence.Valid,
                  originalPrimitiveStreamOpaqueDiscriminationEvidence.PrimitiveRows,
                  Math.Max(1, originalPrimitiveStreamOpaqueDiscriminationEvidence.PrimitiveRows),
                  "bounded raw-word overlap control between fetch commands and validated __VB record/descriptor/payload-prefix bytes; no candidate ordinal produced a nonzero overlap",
                  "stronger independent command/payload identity or decoded record semantics that can discriminate repeated ordinals and establish any attribute/geometry/UV/material binding",
                  $"complete_fetch_rows={originalPrimitiveStreamOpaqueDiscriminationEvidence.CompleteFetchRows}; candidate_ordinal_rows={originalPrimitiveStreamOpaqueDiscriminationEvidence.CandidateOrdinalRows}; stride_matching_candidate_rows={originalPrimitiveStreamOpaqueDiscriminationEvidence.StrideMatchingCandidateRows}; metadata_matching_candidate_rows={originalPrimitiveStreamOpaqueDiscriminationEvidence.MetadataMatchingCandidateRows}; payload_prefix_matching_candidate_rows={originalPrimitiveStreamOpaqueDiscriminationEvidence.PayloadPrefixMatchingCandidateRows}; current_ordinal_rows={originalPrimitiveStreamOpaqueDiscriminationEvidence.CurrentOrdinalRows}; current_ordinal_metadata_matching_rows={originalPrimitiveStreamOpaqueDiscriminationEvidence.CurrentOrdinalMetadataMatchingRows}; current_ordinal_payload_prefix_matching_rows={originalPrimitiveStreamOpaqueDiscriminationEvidence.CurrentOrdinalPayloadPrefixMatchingRows}; uniquely_discriminated_by_metadata_rows={originalPrimitiveStreamOpaqueDiscriminationEvidence.UniquelyDiscriminatedByMetadataRows}; uniquely_discriminated_by_payload_prefix_rows={originalPrimitiveStreamOpaqueDiscriminationEvidence.UniquelyDiscriminatedByPayloadPrefixRows}; shifted_stride_candidate_rows={originalPrimitiveStreamOpaqueDiscriminationEvidence.ShiftedStrideCandidateRows}; shifted_metadata_matching_rows={originalPrimitiveStreamOpaqueDiscriminationEvidence.ShiftedMetadataMatchingRows}; shifted_payload_prefix_matching_rows={originalPrimitiveStreamOpaqueDiscriminationEvidence.ShiftedPayloadPrefixMatchingRows}; rejected_packages={originalPrimitiveStreamOpaqueDiscriminationEvidence.RejectedPackages}; rejected_objects={originalPrimitiveStreamOpaqueDiscriminationEvidence.RejectedObjects}; status={originalPrimitiveStreamOpaqueDiscriminationEvidence.Status}; provenance={originalPrimitiveStreamOpaqueDiscriminationEvidence.Provenance}; control_status={originalPrimitiveStreamOpaqueDiscriminationEvidence.ControlStatus}; control_provenance={originalPrimitiveStreamOpaqueDiscriminationEvidence.ControlProvenance}; original_semantics={originalPrimitiveStreamOpaqueDiscriminationEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-primitive-stream-command-payload-identity",
                  originalPrimitiveStreamCommandPayloadIdentityEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalPrimitiveStreamCommandPayloadIdentityEvidence.Valid,
                  originalPrimitiveStreamCommandPayloadIdentityEvidence.CurrentOrdinalRawIdentityUniqueRows,
                  Math.Max(1, originalPrimitiveStreamCommandPayloadIdentityEvidence.PrimitiveRows),
                  "bounded raw identity hashes for validated __VB record/descriptor/payload head-tail bytes compared with complete raw command-9 packets; exact command/payload matches are a preserved negative control",
                  "decoded command/payload semantics that establish any attribute, geometry, UV, material, skinning, rigging, animation, morph or GPU binding; this control does not promote raw identity",
                  $"complete_fetch_rows={originalPrimitiveStreamCommandPayloadIdentityEvidence.CompleteFetchRows}; candidate_ordinal_rows={originalPrimitiveStreamCommandPayloadIdentityEvidence.CandidateOrdinalRows}; raw_identity_candidate_rows={originalPrimitiveStreamCommandPayloadIdentityEvidence.RawIdentityCandidateRows}; distinct_raw_identity_rows={originalPrimitiveStreamCommandPayloadIdentityEvidence.DistinctRawIdentityRows}; duplicate_raw_identity_rows={originalPrimitiveStreamCommandPayloadIdentityEvidence.DuplicateRawIdentityRows}; command_payload_exact_candidate_rows={originalPrimitiveStreamCommandPayloadIdentityEvidence.CommandPayloadExactCandidateRows}; current_ordinal_rows={originalPrimitiveStreamCommandPayloadIdentityEvidence.CurrentOrdinalRows}; current_ordinal_raw_identity_unique_rows={originalPrimitiveStreamCommandPayloadIdentityEvidence.CurrentOrdinalRawIdentityUniqueRows}; current_ordinal_command_payload_exact_rows={originalPrimitiveStreamCommandPayloadIdentityEvidence.CurrentOrdinalCommandPayloadExactRows}; current_ordinal_uniquely_discriminated_by_command_payload_rows={originalPrimitiveStreamCommandPayloadIdentityEvidence.CurrentOrdinalUniquelyDiscriminatedByCommandPayloadRows}; shifted_ordinal_command_payload_exact_rows={originalPrimitiveStreamCommandPayloadIdentityEvidence.ShiftedOrdinalCommandPayloadExactRows}; rejected_packages={originalPrimitiveStreamCommandPayloadIdentityEvidence.RejectedPackages}; rejected_objects={originalPrimitiveStreamCommandPayloadIdentityEvidence.RejectedObjects}; status={originalPrimitiveStreamCommandPayloadIdentityEvidence.Status}; provenance={originalPrimitiveStreamCommandPayloadIdentityEvidence.Provenance}; control_status={originalPrimitiveStreamCommandPayloadIdentityEvidence.ControlStatus}; control_provenance={originalPrimitiveStreamCommandPayloadIdentityEvidence.ControlProvenance}; original_semantics={originalPrimitiveStreamCommandPayloadIdentityEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-primitive-stream-command-descriptor-adjacency",
                  originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.Valid ? "verified-structural" : "not-present",
                  originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.Valid,
                  originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.UniqueCurrentOrdinalIdentityRows,
                  Math.Max(1, originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.PrimitiveRows),
                  "exact same-section raw offset adjacency: command_stream_offset equals one validated __VB descriptor_offset plus 0x0C for the current ordinal",
                  "independent decoded record semantics and attribute/geometry/UV/material evidence; this association remains structural and does not assign a field or GPU meaning",
                  $"command_descriptor_delta=0x{originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.CommandDescriptorDelta:X}; primitive_rows={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.PrimitiveRows}; complete_command_rows={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.CompleteCommandRows}; candidate_ordinal_rows={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.CandidateOrdinalRows}; identity_candidate_rows={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.IdentityCandidateRows}; current_ordinal_rows={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.CurrentOrdinalRows}; current_ordinal_identity_rows={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.CurrentOrdinalIdentityRows}; unique_current_ordinal_identity_rows={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.UniqueCurrentOrdinalIdentityRows}; shifted_ordinal_identity_rows={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.ShiftedOrdinalIdentityRows}; same_section_identity_rows={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.SameSectionIdentityRows}; exact_adjacency_stream_rows={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.ExactAdjacencyStreamRows}; exact_adjacency_category_histogram={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.ExactAdjacencyCategoryHistogram}; exact_adjacency_stride_histogram={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.ExactAdjacencyStrideHistogram}; rejected_packages={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.RejectedPackages}; rejected_objects={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.RejectedObjects}; status={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.Status}; provenance={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.Provenance}; control_status={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.ControlStatus}; control_provenance={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.ControlProvenance}; original_semantics={originalPrimitiveStreamCommandDescriptorAdjacencyEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-render-payload-object-join",
                  originalRenderPayloadObjectJoinEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalRenderPayloadObjectJoinEvidence.Valid,
                  originalRenderPayloadObjectJoinEvidence.JoinedObjectRows,
                  Math.Max(1, originalRenderPayloadObjectJoinEvidence.RenderObjectRows),
                  "independent object-key join between the raw original EAGL render-chain report and numeric __VB payload report; both reports cover the same .big/.ord objects with coherent aggregate counters",
                  "does not establish primitive-to-stream association or assign geometry, position, UV, material, vertex attribute, skinning, rigging, animation, morph or GPU semantics",
                  $"render_object_rows={originalRenderPayloadObjectJoinEvidence.RenderObjectRows}; payload_object_rows={originalRenderPayloadObjectJoinEvidence.PayloadObjectRows}; joined_object_rows={originalRenderPayloadObjectJoinEvidence.JoinedObjectRows}; render_only_objects={originalRenderPayloadObjectJoinEvidence.RenderOnlyObjects}; payload_only_objects={originalRenderPayloadObjectJoinEvidence.PayloadOnlyObjects}; render_primitive_rows={originalRenderPayloadObjectJoinEvidence.RenderPrimitiveRows}; payload_validated_stream_rows={originalRenderPayloadObjectJoinEvidence.PayloadValidatedStreamRows}; joined_identity_match_rows={originalRenderPayloadObjectJoinEvidence.JoinedIdentityMatchRows}; render_status_valid_rows={originalRenderPayloadObjectJoinEvidence.RenderStatusValidRows}; payload_status_valid_rows={originalRenderPayloadObjectJoinEvidence.PayloadStatusValidRows}; structural_counts_valid_rows={originalRenderPayloadObjectJoinEvidence.StructuralCountsValidRows}; reversed_key_match_rows={originalRenderPayloadObjectJoinEvidence.ReversedKeyMatchRows}; shifted_key_match_rows={originalRenderPayloadObjectJoinEvidence.ShiftedKeyMatchRows}; status={originalRenderPayloadObjectJoinEvidence.Status}; provenance={originalRenderPayloadObjectJoinEvidence.Provenance}; original_semantics={originalRenderPayloadObjectJoinEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-geometry-partition-control",
                  originalStateGeometryPartitionControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateGeometryPartitionControlEvidence.Valid,
                  originalStateGeometryPartitionControlEvidence.StateIndexSetMatchRows,
                  Math.Max(1, originalStateGeometryPartitionControlEvidence.JoinedObjectRows),
                  "independent TSV reparse reconciles object keys, primitive cardinality, state-index coverage and ordered state intervals; object-level exactness remains distinct from geometry-group exactness",
                  "does not assign state runs to mesh groups, materials, UVs, skinning, rigging, animation, morphs, commands or GPU behavior; all original semantics remain unclaimed",
                  $"adjacency_rows={originalStateGeometryPartitionControlEvidence.AdjacencyRows}; profile_rows={originalStateGeometryPartitionControlEvidence.ProfileRows}; order_rows={originalStateGeometryPartitionControlEvidence.OrderRows}; joined_object_rows={originalStateGeometryPartitionControlEvidence.JoinedObjectRows}; adjacency_input_status_valid_rows={originalStateGeometryPartitionControlEvidence.AdjacencyInputStatusValidRows}; profile_input_status_valid_rows={originalStateGeometryPartitionControlEvidence.ProfileInputStatusValidRows}; order_input_status_valid_rows={originalStateGeometryPartitionControlEvidence.OrderInputStatusValidRows}; object_state_exact_rows={originalStateGeometryPartitionControlEvidence.ObjectStateExactRows}; geometry_group_state_exact_rows={originalStateGeometryPartitionControlEvidence.GeometryGroupStateExactRows}; profile_geometry_pair_rows={originalStateGeometryPartitionControlEvidence.ProfileGeometryPairRows}; profile_state_pair_rows={originalStateGeometryPartitionControlEvidence.ProfileStatePairRows}; profile_rows_reconciled={originalStateGeometryPartitionControlEvidence.ProfileRowsReconciled}; order_partitioned_states={originalStateGeometryPartitionControlEvidence.OrderPartitionedStates}; adjacency_states={originalStateGeometryPartitionControlEvidence.AdjacencyStates}; order_states={originalStateGeometryPartitionControlEvidence.OrderStates}; state_index_set_match_rows={originalStateGeometryPartitionControlEvidence.StateIndexSetMatchRows}; interval_index_sequence_match_rows={originalStateGeometryPartitionControlEvidence.IntervalIndexSequenceMatchRows}; interval_boundary_shape_match_rows={originalStateGeometryPartitionControlEvidence.IntervalBoundaryShapeMatchRows}; complete_profile_stream_rows={originalStateGeometryPartitionControlEvidence.CompleteProfileStreamRows}; exact_command_shape_rows={originalStateGeometryPartitionControlEvidence.ExactCommandShapeRows}; exact_command_shape_expected_zero={originalStateGeometryPartitionControlEvidence.ExactCommandShapeExpectedZero}; status={originalStateGeometryPartitionControlEvidence.Status}; provenance={originalStateGeometryPartitionControlEvidence.Provenance}; original_semantics={originalStateGeometryPartitionControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-geometry-binding-ambiguity-control",
                  originalStateGeometryBindingAmbiguityControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateGeometryBindingAmbiguityControlEvidence.Valid,
                  originalStateGeometryBindingAmbiguityControlEvidence.ValidObjectRows,
                  Math.Max(1, originalStateGeometryBindingAmbiguityControlEvidence.JoinedObjectRows),
                  "independent TSV reparse reconciles profile/order/partition cardinalities and explicitly measures unresolved state-to-geometry binding ambiguity",
                  "does not assign state runs to draw calls, mesh groups, UVs, materials, skinning, rigging, animation, morphs, commands or GPU behavior",
                  $"adjacency_rows={originalStateGeometryBindingAmbiguityControlEvidence.AdjacencyRows}; profile_rows={originalStateGeometryBindingAmbiguityControlEvidence.ProfileRows}; order_rows={originalStateGeometryBindingAmbiguityControlEvidence.OrderRows}; partition_rows={originalStateGeometryBindingAmbiguityControlEvidence.PartitionRows}; joined_object_rows={originalStateGeometryBindingAmbiguityControlEvidence.JoinedObjectRows}; missing_profile_objects={originalStateGeometryBindingAmbiguityControlEvidence.MissingProfileObjects}; missing_order_objects={originalStateGeometryBindingAmbiguityControlEvidence.MissingOrderObjects}; missing_partition_objects={originalStateGeometryBindingAmbiguityControlEvidence.MissingPartitionObjects}; adjacency_input_status_valid_rows={originalStateGeometryBindingAmbiguityControlEvidence.AdjacencyInputStatusValidRows}; profile_input_status_valid_rows={originalStateGeometryBindingAmbiguityControlEvidence.ProfileInputStatusValidRows}; order_input_status_valid_rows={originalStateGeometryBindingAmbiguityControlEvidence.OrderInputStatusValidRows}; partition_input_status_valid_rows={originalStateGeometryBindingAmbiguityControlEvidence.PartitionInputStatusValidRows}; profile_cardinality_reconciled_rows={originalStateGeometryBindingAmbiguityControlEvidence.ProfileCardinalityReconciledRows}; order_cardinality_reconciled_rows={originalStateGeometryBindingAmbiguityControlEvidence.OrderCardinalityReconciledRows}; order_state_index_set_reconciled_rows={originalStateGeometryBindingAmbiguityControlEvidence.OrderStateIndexSetReconciledRows}; state_count_greater_than_geometry_rows={originalStateGeometryBindingAmbiguityControlEvidence.StateCountGreaterThanGeometryRows}; geometry_group_state_exact_rows={originalStateGeometryBindingAmbiguityControlEvidence.GeometryGroupStateExactRows}; exact_command_shape_rows={originalStateGeometryBindingAmbiguityControlEvidence.ExactCommandShapeRows}; unique_geometry_state_binding_proven_rows={originalStateGeometryBindingAmbiguityControlEvidence.UniqueGeometryStateBindingProvenRows}; valid_object_rows={originalStateGeometryBindingAmbiguityControlEvidence.ValidObjectRows}; status={originalStateGeometryBindingAmbiguityControlEvidence.Status}; semantic_binding={originalStateGeometryBindingAmbiguityControlEvidence.SemanticBinding}; provenance={originalStateGeometryBindingAmbiguityControlEvidence.Provenance}; original_semantics={originalStateGeometryBindingAmbiguityControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-geometry-global-assignment-control",
                  originalStateGeometryGlobalAssignmentControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateGeometryGlobalAssignmentControlEvidence.Valid,
                  originalStateGeometryGlobalAssignmentControlEvidence.ValidObjectRows,
                  Math.Max(1, originalStateGeometryGlobalAssignmentControlEvidence.JoinedObjectRows),
                  "independent enumeration of every state-run assignment allowed by observed ordered interval boundaries; singleton domains remain non-semantic and one-to-one cardinality is diagnostic only",
                  "decoded state ownership or draw-call evidence that selects a geometry candidate without assuming previous/next ownership or one-to-one cardinality",
                  $"order_rows={originalStateGeometryGlobalAssignmentControlEvidence.OrderRows}; partition_rows={originalStateGeometryGlobalAssignmentControlEvidence.PartitionRows}; joined_object_rows={originalStateGeometryGlobalAssignmentControlEvidence.JoinedObjectRows}; missing_partition_objects={originalStateGeometryGlobalAssignmentControlEvidence.MissingPartitionObjects}; order_input_status_valid_rows={originalStateGeometryGlobalAssignmentControlEvidence.OrderInputStatusValidRows}; partition_input_status_valid_rows={originalStateGeometryGlobalAssignmentControlEvidence.PartitionInputStatusValidRows}; nonempty_interval_rows={originalStateGeometryGlobalAssignmentControlEvidence.NonemptyIntervalRows}; ambiguous_interval_rows={originalStateGeometryGlobalAssignmentControlEvidence.AmbiguousIntervalRows}; objects_with_multiple_assignment_models={originalStateGeometryGlobalAssignmentControlEvidence.ObjectsWithMultipleAssignmentModels}; objects_with_unique_assignment_model={originalStateGeometryGlobalAssignmentControlEvidence.ObjectsWithUniqueAssignmentModel}; objects_with_one_to_one_unique_model={originalStateGeometryGlobalAssignmentControlEvidence.ObjectsWithOneToOneUniqueModel}; unconstrained_assignment_model_sum={originalStateGeometryGlobalAssignmentControlEvidence.UnconstrainedAssignmentModelSum}; one_to_one_assignment_model_sum={originalStateGeometryGlobalAssignmentControlEvidence.OneToOneAssignmentModelSum}; state_index_set_reconciled_rows={originalStateGeometryGlobalAssignmentControlEvidence.StateIndexSetReconciledRows}; unique_state_geometry_assignment_proven_rows={originalStateGeometryGlobalAssignmentControlEvidence.UniqueStateGeometryAssignmentProvenRows}; valid_object_rows={originalStateGeometryGlobalAssignmentControlEvidence.ValidObjectRows}; status={originalStateGeometryGlobalAssignmentControlEvidence.Status}; semantic_binding={originalStateGeometryGlobalAssignmentControlEvidence.SemanticBinding}; provenance={originalStateGeometryGlobalAssignmentControlEvidence.Provenance}; original_semantics={originalStateGeometryGlobalAssignmentControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-geometry-uv-material-global-assignment-control",
                  originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.Valid,
                  originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.ValidObjectRows,
                  Math.Max(1, originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.JoinedObjectRows),
                  "independent keyed reconciliation enumerates the global state/geometry models together with candidate slot-1 role and same-object material-source domains; singleton candidate domains remain non-semantic",
                  "decoded state ownership, UV interpretation or material binding that selects one complete render model without promoting candidate slot/raster/source-chain evidence",
                  $"global_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.GlobalRows}; uv_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.UvRows}; material_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.MaterialRows}; joined_object_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.JoinedObjectRows}; missing_uv_objects={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.MissingUvObjects}; missing_material_objects={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.MissingMaterialObjects}; global_input_status_valid_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.GlobalInputStatusValidRows}; uv_candidate_attribute_valid_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.UvCandidateAttributeValidRows}; uv_candidate_raster_influence_valid_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.UvCandidateRasterInfluenceValidRows}; uv_complete_group_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.UvCompleteGroupRows}; uv_incomplete_group_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.UvIncompleteGroupRows}; uv_slot1_near_unit_group_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.UvSlot1NearUnitGroupRows}; uv_other_slot_near_unit_group_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.UvOtherSlotNearUnitGroupRows}; material_input_status_valid_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.MaterialInputStatusValidRows}; material_candidate_asset_valid_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.MaterialCandidateAssetValidRows}; material_state_candidate_source_complete_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.MaterialStateCandidateSourceCompleteRows}; objects_with_multiple_geometry_assignment_models={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.ObjectsWithMultipleGeometryAssignmentModels}; objects_with_single_geometry_assignment_model={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.ObjectsWithSingleGeometryAssignmentModel}; objects_with_single_candidate_uv_role_domain={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.ObjectsWithSingleCandidateUvRoleDomain}; objects_with_single_candidate_material_domain={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.ObjectsWithSingleCandidateMaterialDomain}; structural_cross_product_model_sum={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.StructuralCrossProductModelSum}; objects_with_multiple_structural_cross_product_models={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.ObjectsWithMultipleStructuralCrossProductModels}; objects_with_single_structural_cross_product_model={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.ObjectsWithSingleStructuralCrossProductModel}; unique_state_geometry_uv_material_binding_proven_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.UniqueStateGeometryUvMaterialBindingProvenRows}; valid_object_rows={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.ValidObjectRows}; status={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.Status}; semantic_binding={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.SemanticBinding}; provenance={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.Provenance}; original_semantics={originalStateGeometryUvMaterialGlobalAssignmentControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-geometry-candidate-render-global-assignment-control",
                  originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.Valid,
                  originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.ValidObjectRows,
                  Math.Max(1, originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.JoinedObjectRows),
                  "independent object-key reconciliation compares global original state/geometry models with candidate group/source-chain and candidate runtime object state; candidate render-group state remains an explicit negative",
                  "decoded original state ownership, candidate render-group ownership, UV/material semantics or a unique state-to-draw binding; object_state exactness is not render_group_state evidence",
                  $"global_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.GlobalRows}; runtime_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.RuntimeRows}; candidate_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.CandidateRows}; payload_join_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.PayloadJoinRows}; joined_object_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.JoinedObjectRows}; missing_runtime_objects={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.MissingRuntimeObjects}; missing_candidate_objects={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.MissingCandidateObjects}; missing_payload_join_objects={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.MissingPayloadJoinObjects}; global_input_status_valid_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.GlobalInputStatusValidRows}; runtime_input_status_valid_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.RuntimeInputStatusValidRows}; candidate_input_status_valid_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.CandidateInputStatusValidRows}; payload_join_input_status_valid_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.PayloadJoinInputStatusValidRows}; geometry_candidate_count_sum={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.GeometryCandidateCountSum}; candidate_geometry_group_sum={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.CandidateGeometryGroupSum}; geometry_candidate_cardinality_match_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.GeometryCandidateCardinalityMatchRows}; object_state_exact_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.ObjectStateExactRows}; render_group_state_match_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.RenderGroupStateMatchRows}; candidate_source_chain_valid_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.CandidateSourceChainValidRows}; payload_object_join_valid_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.PayloadObjectJoinValidRows}; objects_with_multiple_geometry_assignment_models={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.ObjectsWithMultipleGeometryAssignmentModels}; objects_with_single_geometry_assignment_models={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.ObjectsWithSingleGeometryAssignmentModels}; unconstrained_assignment_model_sum={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.UnconstrainedAssignmentModelSum}; one_to_one_assignment_model_sum={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.OneToOneAssignmentModelSum}; unique_state_geometry_candidate_render_binding_proven_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.UniqueStateGeometryCandidateRenderBindingProvenRows}; valid_object_rows={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.ValidObjectRows}; status={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.Status}; semantic_binding={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.SemanticBinding}; provenance={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.Provenance}; original_semantics={originalStateGeometryCandidateRenderGlobalAssignmentControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-geometry-payload-identity-global-assignment-control",
                  originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.Valid,
                  originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.ValidObjectRows,
                  Math.Max(1, originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.JoinedObjectRows),
                  "independent object-key reconciliation tests whether exact original/candidate geometry payload identity reduces the global state-to-geometry assignment domain",
                  "does not establish state ownership: exact payload identity remains geometry-candidate evidence and does not select a state, draw, UV, material or GPU binding",
                  $"global_rows={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.GlobalRows}; payload_identity_rows={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.PayloadIdentityRows}; joined_object_rows={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.JoinedObjectRows}; missing_payload_identity_objects={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.MissingPayloadIdentityObjects}; global_input_status_valid_rows={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.GlobalInputStatusValidRows}; payload_identity_input_status_valid_rows={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.PayloadIdentityInputStatusValidRows}; geometry_candidate_count_sum={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.GeometryCandidateCountSum}; payload_identity_geometry_row_sum={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.PayloadIdentityGeometryRowSum}; payload_identity_complete_geometry_sum={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.PayloadIdentityCompleteGeometrySum}; payload_identity_raw_hash_row_sum={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.PayloadIdentityRawHashRowSum}; payload_identity_cardinality_match_rows={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.PayloadIdentityCardinalityMatchRows}; payload_identity_reduced_state_domain_rows={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.PayloadIdentityReducedStateDomainRows}; objects_with_multiple_assignment_models={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.ObjectsWithMultipleAssignmentModels}; objects_with_single_assignment_models={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.ObjectsWithSingleAssignmentModels}; assignment_model_sum_before_payload_identity={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.AssignmentModelSumBeforePayloadIdentity}; assignment_model_sum_after_payload_identity={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.AssignmentModelSumAfterPayloadIdentity}; one_to_one_model_sum_before_payload_identity={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.OneToOneModelSumBeforePayloadIdentity}; one_to_one_model_sum_after_payload_identity={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.OneToOneModelSumAfterPayloadIdentity}; unique_state_geometry_payload_binding_proven_rows={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.UniqueStateGeometryPayloadBindingProvenRows}; valid_object_rows={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.ValidObjectRows}; status={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.Status}; semantic_binding={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.SemanticBinding}; provenance={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.Provenance}; original_semantics={originalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-geometry-draw-classification-global-assignment-control",
                  originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.Valid,
                  originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.ValidObjectRows,
                  Math.Max(1, originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.JoinedObjectRows),
                  "independent primitive-key reconciliation matches the 38 geometry draw-classification rows to candidate payload identities while preserving the 180 state-marker rows outside that payload domain",
                  "does not establish state ownership: geometry/draw classification and payload identity do not select a state, draw, UV, material or GPU binding",
                  $"global_rows={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.GlobalRows}; draw_classification_rows={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.DrawClassificationRows}; payload_identity_rows={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.PayloadIdentityRows}; joined_object_rows={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.JoinedObjectRows}; missing_draw_classification_objects={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.MissingDrawClassificationObjects}; missing_payload_identity_objects={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.MissingPayloadIdentityObjects}; global_input_status_valid_rows={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.GlobalInputStatusValidRows}; draw_classification_input_status_valid_rows={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.DrawClassificationInputStatusValidRows}; payload_identity_input_status_valid_rows={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.PayloadIdentityInputStatusValidRows}; geometry_candidate_count_sum={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.GeometryCandidateCountSum}; draw_classification_geometry_marker_sum={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.DrawClassificationGeometryMarkerSum}; payload_identity_geometry_row_sum={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.PayloadIdentityGeometryRowSum}; classification_match_rows={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.ClassificationMatchRows}; geometry_draw_payload_identity_join_rows={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.GeometryDrawPayloadIdentityJoinRows}; state_marker_rows={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.StateMarkerRows}; state_rows_without_payload_identity={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.StateRowsWithoutPayloadIdentity}; objects_with_state_over_geometry_rows={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.ObjectsWithStateOverGeometryRows}; assignment_model_sum_before_draw_classification={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.AssignmentModelSumBeforeDrawClassification}; assignment_model_sum_after_draw_classification={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.AssignmentModelSumAfterDrawClassification}; one_to_one_model_sum_before_draw_classification={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.OneToOneModelSumBeforeDrawClassification}; one_to_one_model_sum_after_draw_classification={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.OneToOneModelSumAfterDrawClassification}; draw_classification_reduced_state_domain_rows={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.DrawClassificationReducedStateDomainRows}; unique_state_geometry_draw_binding_proven_rows={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.UniqueStateGeometryDrawBindingProvenRows}; valid_object_rows={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.ValidObjectRows}; status={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.Status}; semantic_binding={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.SemanticBinding}; provenance={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.Provenance}; original_semantics={originalStateGeometryDrawClassificationGlobalAssignmentControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-runtime-uid-ambiguity-control",
                  originalStateUidAmbiguityControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateUidAmbiguityControlEvidence.Valid,
                  originalStateUidAmbiguityControlEvidence.GeometryRowsSameUidStateCandidates,
                  Math.Max(1, originalStateUidAmbiguityControlEvidence.GeometryRows),
                  "independent UID-token reparse shows that every candidate geometry row shares a RUNTIME_ALLOC UID with state rows, but each UID maps to multiple state candidates",
                  "does not interpret RUNTIME_ALLOC UID as a draw, material, UV, geometry, skinning, rigging, animation, morph or GPU binding",
                  $"candidate_object_rows={originalStateUidAmbiguityControlEvidence.CandidateObjectRows}; render_scoped_rows={originalStateUidAmbiguityControlEvidence.RenderScopedRows}; joined_object_rows={originalStateUidAmbiguityControlEvidence.JoinedObjectRows}; missing_candidate_objects={originalStateUidAmbiguityControlEvidence.MissingCandidateObjects}; missing_render_objects={originalStateUidAmbiguityControlEvidence.MissingRenderObjects}; geometry_rows={originalStateUidAmbiguityControlEvidence.GeometryRows}; state_rows={originalStateUidAmbiguityControlEvidence.StateRows}; geometry_rows_with_valid_uid={originalStateUidAmbiguityControlEvidence.GeometryRowsWithValidUid}; state_rows_with_valid_uid={originalStateUidAmbiguityControlEvidence.StateRowsWithValidUid}; shared_uid_count={originalStateUidAmbiguityControlEvidence.SharedUidCount}; geometry_rows_same_uid_state_candidates={originalStateUidAmbiguityControlEvidence.GeometryRowsSameUidStateCandidates}; geometry_rows_zero_same_uid_state_candidates={originalStateUidAmbiguityControlEvidence.GeometryRowsZeroSameUidStateCandidates}; geometry_rows_one_same_uid_state_candidate={originalStateUidAmbiguityControlEvidence.GeometryRowsOneSameUidStateCandidate}; geometry_rows_multiple_same_uid_state_candidates={originalStateUidAmbiguityControlEvidence.GeometryRowsMultipleSameUidStateCandidates}; state_rows_in_shared_uid_domains={originalStateUidAmbiguityControlEvidence.StateRowsInSharedUidDomains}; objects_with_uid_ambiguity={originalStateUidAmbiguityControlEvidence.ObjectsWithUidAmbiguity}; unique_geometry_state_binding_proven_rows={originalStateUidAmbiguityControlEvidence.UniqueGeometryStateBindingProvenRows}; candidate_input_status_valid_rows={originalStateUidAmbiguityControlEvidence.CandidateInputStatusValidRows}; render_input_status_valid_rows={originalStateUidAmbiguityControlEvidence.RenderInputStatusValidRows}; valid_object_rows={originalStateUidAmbiguityControlEvidence.ValidObjectRows}; status={originalStateUidAmbiguityControlEvidence.Status}; semantic_binding={originalStateUidAmbiguityControlEvidence.SemanticBinding}; provenance={originalStateUidAmbiguityControlEvidence.Provenance}; original_semantics={originalStateUidAmbiguityControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-uid-command-hash-ambiguity-control",
                  originalStateUidCommandHashAmbiguityControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateUidCommandHashAmbiguityControlEvidence.Valid,
                  originalStateUidCommandHashAmbiguityControlEvidence.GeometryRowsSameUidStateCandidates,
                  Math.Max(1, originalStateUidCommandHashAmbiguityControlEvidence.GeometryRows),
                  "independent UID and raw command-stream SHA-256 reparse shows that UID sharing remains ambiguous and the command hash adds no state candidate",
                  "does not interpret RUNTIME_ALLOC UID or command bytes as a draw, material, UV, geometry, skinning, rigging, animation, morph or GPU binding",
                  $"candidate_object_rows={originalStateUidCommandHashAmbiguityControlEvidence.CandidateObjectRows}; render_scoped_rows={originalStateUidCommandHashAmbiguityControlEvidence.RenderScopedRows}; joined_object_rows={originalStateUidCommandHashAmbiguityControlEvidence.JoinedObjectRows}; missing_candidate_objects={originalStateUidCommandHashAmbiguityControlEvidence.MissingCandidateObjects}; missing_render_objects={originalStateUidCommandHashAmbiguityControlEvidence.MissingRenderObjects}; geometry_rows={originalStateUidCommandHashAmbiguityControlEvidence.GeometryRows}; state_rows={originalStateUidCommandHashAmbiguityControlEvidence.StateRows}; geometry_rows_with_valid_uid={originalStateUidCommandHashAmbiguityControlEvidence.GeometryRowsWithValidUid}; state_rows_with_valid_uid={originalStateUidCommandHashAmbiguityControlEvidence.StateRowsWithValidUid}; shared_uid_count={originalStateUidCommandHashAmbiguityControlEvidence.SharedUidCount}; geometry_rows_same_uid_state_candidates={originalStateUidCommandHashAmbiguityControlEvidence.GeometryRowsSameUidStateCandidates}; geometry_rows_zero_same_uid_state_candidates={originalStateUidCommandHashAmbiguityControlEvidence.GeometryRowsZeroSameUidStateCandidates}; geometry_rows_one_same_uid_state_candidate={originalStateUidCommandHashAmbiguityControlEvidence.GeometryRowsOneSameUidStateCandidate}; geometry_rows_multiple_same_uid_state_candidates={originalStateUidCommandHashAmbiguityControlEvidence.GeometryRowsMultipleSameUidStateCandidates}; geometry_rows_with_uid_and_command_hash_candidates={originalStateUidCommandHashAmbiguityControlEvidence.GeometryRowsWithUidAndCommandHashCandidates}; geometry_rows_with_zero_uid_and_command_hash_candidates={originalStateUidCommandHashAmbiguityControlEvidence.GeometryRowsWithZeroUidAndCommandHashCandidates}; geometry_rows_with_one_uid_and_command_hash_candidate={originalStateUidCommandHashAmbiguityControlEvidence.GeometryRowsWithOneUidAndCommandHashCandidate}; geometry_rows_with_multiple_uid_and_command_hash_candidates={originalStateUidCommandHashAmbiguityControlEvidence.GeometryRowsWithMultipleUidAndCommandHashCandidates}; state_rows_in_shared_uid_domains={originalStateUidCommandHashAmbiguityControlEvidence.StateRowsInSharedUidDomains}; objects_with_uid_ambiguity={originalStateUidCommandHashAmbiguityControlEvidence.ObjectsWithUidAmbiguity}; objects_with_uid_and_command_hash_ambiguity={originalStateUidCommandHashAmbiguityControlEvidence.ObjectsWithUidAndCommandHashAmbiguity}; unique_geometry_state_binding_proven_rows={originalStateUidCommandHashAmbiguityControlEvidence.UniqueGeometryStateBindingProvenRows}; unique_geometry_state_binding_with_command_hash_proven_rows={originalStateUidCommandHashAmbiguityControlEvidence.UniqueGeometryStateBindingWithCommandHashProvenRows}; candidate_input_status_valid_rows={originalStateUidCommandHashAmbiguityControlEvidence.CandidateInputStatusValidRows}; render_input_status_valid_rows={originalStateUidCommandHashAmbiguityControlEvidence.RenderInputStatusValidRows}; valid_object_rows={originalStateUidCommandHashAmbiguityControlEvidence.ValidObjectRows}; status={originalStateUidCommandHashAmbiguityControlEvidence.Status}; semantic_binding={originalStateUidCommandHashAmbiguityControlEvidence.SemanticBinding}; provenance={originalStateUidCommandHashAmbiguityControlEvidence.Provenance}; original_semantics={originalStateUidCommandHashAmbiguityControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-profile-discrimination-control",
                  originalStateProfileDiscriminationControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateProfileDiscriminationControlEvidence.Valid,
                  originalStateProfileDiscriminationControlEvidence.ValidObjectRows,
                  Math.Max(1, originalStateProfileDiscriminationControlEvidence.ProfileObjects),
                  "independent state-identity reparse recomputes profile ties and shows which original state primitives remain compatible with multiple geometry candidates",
                  "does not promote state profile equality or a single-geometry object to state-to-draw, UV, material, command or GPU semantics",
                  $"profile_rows={originalStateProfileDiscriminationControlEvidence.ProfileRows}; profile_objects={originalStateProfileDiscriminationControlEvidence.ProfileObjects}; geometry_candidate_rows={originalStateProfileDiscriminationControlEvidence.GeometryCandidateRows}; state_primitive_rows={originalStateProfileDiscriminationControlEvidence.StatePrimitiveRows}; distinct_object_state_profile_signatures={originalStateProfileDiscriminationControlEvidence.DistinctObjectStateProfileSignatures}; profile_tie_count_match_rows={originalStateProfileDiscriminationControlEvidence.ProfileTieCountMatchRows}; state_primitives_with_multiple_geometry_candidates={originalStateProfileDiscriminationControlEvidence.StatePrimitivesWithMultipleGeometryCandidates}; state_primitives_with_single_geometry_candidate={originalStateProfileDiscriminationControlEvidence.StatePrimitivesWithSingleGeometryCandidate}; state_primitives_with_zero_geometry_candidates={originalStateProfileDiscriminationControlEvidence.StatePrimitivesWithZeroGeometryCandidates}; state_signature_groups_with_multiple_geometry_candidates={originalStateProfileDiscriminationControlEvidence.StateSignatureGroupsWithMultipleGeometryCandidates}; exact_command_shape_rows={originalStateProfileDiscriminationControlEvidence.ExactCommandShapeRows}; unique_state_to_geometry_binding_proven_rows={originalStateProfileDiscriminationControlEvidence.UniqueStateToGeometryBindingProvenRows}; status={originalStateProfileDiscriminationControlEvidence.Status}; semantic_binding={originalStateProfileDiscriminationControlEvidence.SemanticBinding}; provenance={originalStateProfileDiscriminationControlEvidence.Provenance}; original_semantics={originalStateProfileDiscriminationControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-slot1-candidate-uv-ambiguity-control",
                  originalStateSlot1CandidateUvAmbiguityEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateSlot1CandidateUvAmbiguityEvidence.Valid,
                  originalStateSlot1CandidateUvAmbiguityEvidence.ValidObjectRows,
                  Math.Max(1, originalStateSlot1CandidateUvAmbiguityEvidence.CandidateObjectRows),
                  "independent primitive-key state/fetch and numeric-slot reparse intersects candidate texture/raster validation while preserving state-to-geometry ambiguity",
                  "does not call numeric slot 1 UV, does not establish state-to-draw or material semantics, and does not promote candidate texture/raster evidence to original semantics",
                  $"state_rows={originalStateSlot1CandidateUvAmbiguityEvidence.StateRows}; role_rows={originalStateSlot1CandidateUvAmbiguityEvidence.RoleRows}; joined_primitive_groups={originalStateSlot1CandidateUvAmbiguityEvidence.JoinedPrimitiveGroups}; candidate_joined_primitive_groups={originalStateSlot1CandidateUvAmbiguityEvidence.CandidateJoinedPrimitiveGroups}; missing_state_keys={originalStateSlot1CandidateUvAmbiguityEvidence.MissingStateKeys}; missing_role_keys={originalStateSlot1CandidateUvAmbiguityEvidence.MissingRoleKeys}; duplicate_state_keys={originalStateSlot1CandidateUvAmbiguityEvidence.DuplicateStateKeys}; duplicate_role_keys={originalStateSlot1CandidateUvAmbiguityEvidence.DuplicateRoleKeys}; complete_five_slot_groups={originalStateSlot1CandidateUvAmbiguityEvidence.CompleteFiveSlotGroups}; incomplete_five_slot_groups={originalStateSlot1CandidateUvAmbiguityEvidence.IncompleteFiveSlotGroups}; shapename_state_groups={originalStateSlot1CandidateUvAmbiguityEvidence.ShapenameStateGroups}; complete_shapename_state_groups={originalStateSlot1CandidateUvAmbiguityEvidence.CompleteShapenameStateGroups}; complete_shapename_slot1_near_unit_groups={originalStateSlot1CandidateUvAmbiguityEvidence.CompleteShapenameSlot1NearUnitGroups}; complete_shapename_other_slot_near_unit_groups={originalStateSlot1CandidateUvAmbiguityEvidence.CompleteShapenameOtherSlotNearUnitGroups}; complete_groups_slot1_near_unit_groups={originalStateSlot1CandidateUvAmbiguityEvidence.CompleteGroupsSlot1NearUnitGroups}; complete_groups_other_slot_near_unit_groups={originalStateSlot1CandidateUvAmbiguityEvidence.CompleteGroupsOtherSlotNearUnitGroups}; candidate_object_rows={originalStateSlot1CandidateUvAmbiguityEvidence.CandidateObjectRows}; candidate_attribute_texture_valid_rows={originalStateSlot1CandidateUvAmbiguityEvidence.CandidateAttributeTextureValidRows}; candidate_raster_influence_valid_rows={originalStateSlot1CandidateUvAmbiguityEvidence.CandidateRasterInfluenceValidRows}; unique_state_to_geometry_binding_proven_rows={originalStateSlot1CandidateUvAmbiguityEvidence.UniqueStateToGeometryBindingProvenRows}; valid_object_rows={originalStateSlot1CandidateUvAmbiguityEvidence.ValidObjectRows}; status={originalStateSlot1CandidateUvAmbiguityEvidence.Status}; semantic_binding={originalStateSlot1CandidateUvAmbiguityEvidence.SemanticBinding}; provenance={originalStateSlot1CandidateUvAmbiguityEvidence.Provenance}; original_semantics={originalStateSlot1CandidateUvAmbiguityEvidence.OriginalSemantics}"),
              new(
                  "rendering-original-state-geometry-payload-join-control",
                  originalStateGeometryPayloadJoinControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  originalStateGeometryPayloadJoinControlEvidence.Valid,
                  originalStateGeometryPayloadJoinControlEvidence.PrimitiveCountMatchRows,
                  Math.Max(1, originalStateGeometryPayloadJoinControlEvidence.JoinedObjectRows),
                  "independent object-key join places all state/geometry objects in the validated original render/payload object report and reconciles primitive and five-slot counters",
                  "does not assign payload records to positions, UVs, materials, vertex attributes, skinning, rigging, animation, morphs, commands or GPU behavior",
                  $"adjacency_rows={originalStateGeometryPayloadJoinControlEvidence.AdjacencyRows}; join_rows={originalStateGeometryPayloadJoinControlEvidence.JoinRows}; joined_object_rows={originalStateGeometryPayloadJoinControlEvidence.JoinedObjectRows}; missing_join_objects={originalStateGeometryPayloadJoinControlEvidence.MissingJoinObjects}; adjacency_input_status_valid_rows={originalStateGeometryPayloadJoinControlEvidence.AdjacencyInputStatusValidRows}; join_input_status_valid_rows={originalStateGeometryPayloadJoinControlEvidence.JoinInputStatusValidRows}; identity_join_match_rows={originalStateGeometryPayloadJoinControlEvidence.IdentityJoinMatchRows}; render_status_valid_rows={originalStateGeometryPayloadJoinControlEvidence.RenderStatusValidRows}; payload_status_valid_rows={originalStateGeometryPayloadJoinControlEvidence.PayloadStatusValidRows}; structural_counts_valid_rows={originalStateGeometryPayloadJoinControlEvidence.StructuralCountsValidRows}; primitive_count_match_rows={originalStateGeometryPayloadJoinControlEvidence.PrimitiveCountMatchRows}; complete_command_count_match_rows={originalStateGeometryPayloadJoinControlEvidence.CompleteCommandCountMatchRows}; fetch_count_match_rows={originalStateGeometryPayloadJoinControlEvidence.FetchCountMatchRows}; five_slot_payload_count_match_rows={originalStateGeometryPayloadJoinControlEvidence.FiveSlotPayloadCountMatchRows}; distinct_payload_range_count_match_rows={originalStateGeometryPayloadJoinControlEvidence.DistinctPayloadRangeCountMatchRows}; payload_overlap_zero_rows={originalStateGeometryPayloadJoinControlEvidence.PayloadOverlapZeroRows}; status={originalStateGeometryPayloadJoinControlEvidence.Status}; provenance={originalStateGeometryPayloadJoinControlEvidence.Provenance}; original_semantics={originalStateGeometryPayloadJoinControlEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-indexed-cfg",
                  staticRenderIndirectIndexedCfgEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderIndirectIndexedCfgEvidence.Valid,
                  staticRenderIndirectIndexedCfgEvidence.TargetsWithReachableCfg,
                  1,
                  "bounded CFG reachability over distinct indexed-table candidate windows; direct calls and indirect transfers remain unresolved",
                  "independent table length/pointer/function recovery and verified render-state-to-geometry/UV/material binding evidence; CFG slices do not establish function boundaries",
                  $"candidate_targets={staticRenderIndirectIndexedCfgEvidence.CandidateTargets}; analyzed_targets={staticRenderIndirectIndexedCfgEvidence.AnalyzedTargets}; targets_with_reachable_cfg={staticRenderIndirectIndexedCfgEvidence.TargetsWithReachableCfg}; targets_with_reachable_ret={staticRenderIndirectIndexedCfgEvidence.TargetsWithReachableRet}; total_linear_instructions={staticRenderIndirectIndexedCfgEvidence.TotalLinearInstructions}; total_reachable_instructions={staticRenderIndirectIndexedCfgEvidence.TotalReachableInstructions}; total_basic_blocks={staticRenderIndirectIndexedCfgEvidence.TotalBasicBlocks}; total_conditional_branches={staticRenderIndirectIndexedCfgEvidence.TotalConditionalBranches}; total_unconditional_branches={staticRenderIndirectIndexedCfgEvidence.TotalUnconditionalBranches}; total_internal_branch_targets={staticRenderIndirectIndexedCfgEvidence.TotalInternalBranchTargets}; total_out_of_window_branch_targets={staticRenderIndirectIndexedCfgEvidence.TotalOutOfWindowBranchTargets}; total_direct_calls={staticRenderIndirectIndexedCfgEvidence.TotalDirectCalls}; total_unique_direct_call_targets={staticRenderIndirectIndexedCfgEvidence.TotalUniqueDirectCallTargets}; distinct_direct_call_targets={staticRenderIndirectIndexedCfgEvidence.DistinctDirectCallTargets}; total_indirect_control_transfers={staticRenderIndirectIndexedCfgEvidence.TotalIndirectControlTransfers}; status={staticRenderIndirectIndexedCfgEvidence.Status}; provenance={staticRenderIndirectIndexedCfgEvidence.Provenance}; original_semantics={staticRenderIndirectIndexedCfgEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-indexed-cfg-call-targets",
                  staticRenderIndirectIndexedCfgCallTargetEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderIndirectIndexedCfgCallTargetEvidence.Valid,
                  staticRenderIndirectIndexedCfgCallTargetEvidence.DisassembledTargets,
                  1,
                  "bounded executable-section slices at direct-call destinations observed in indexed CFGs",
                  "independent function-boundary and call-following evidence plus verified render-state-to-geometry/UV/material binding evidence; call destinations remain candidates",
                  $"cfg_direct_call_occurrences={staticRenderIndirectIndexedCfgCallTargetEvidence.CfgDirectCallOccurrences}; cfg_unique_direct_call_targets_sum={staticRenderIndirectIndexedCfgCallTargetEvidence.CfgUniqueDirectCallTargetsSum}; distinct_direct_call_targets={staticRenderIndirectIndexedCfgCallTargetEvidence.DistinctDirectCallTargets}; mapped_executable_targets={staticRenderIndirectIndexedCfgCallTargetEvidence.MappedExecutableTargets}; disassembled_targets={staticRenderIndirectIndexedCfgCallTargetEvidence.DisassembledTargets}; ret_terminated_targets={staticRenderIndirectIndexedCfgCallTargetEvidence.RetTerminatedTargets}; prologue_like_targets={staticRenderIndirectIndexedCfgCallTargetEvidence.PrologueLikeTargets}; status={staticRenderIndirectIndexedCfgCallTargetEvidence.Status}; provenance={staticRenderIndirectIndexedCfgCallTargetEvidence.Provenance}; original_semantics={staticRenderIndirectIndexedCfgCallTargetEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-indexed-cfg-call-sites",
                  staticRenderIndirectIndexedCfgCallSiteEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderIndirectIndexedCfgCallSiteEvidence.Valid,
                  staticRenderIndirectIndexedCfgCallSiteEvidence.DirectCallOccurrences,
                  24,
                  "bounded local instruction context around reachable immediate calls in indexed CFG slices",
                  "independent function-boundary, pointer/object-layout and semantic evidence; repeated call-site shape remains structural only",
                  $"source_cfg_candidates={staticRenderIndirectIndexedCfgCallSiteEvidence.SourceCfgCandidates}; direct_call_occurrences={staticRenderIndirectIndexedCfgCallSiteEvidence.DirectCallOccurrences}; distinct_call_targets={staticRenderIndirectIndexedCfgCallSiteEvidence.DistinctCallTargets}; candidate_targets_with_call_sites={staticRenderIndirectIndexedCfgCallSiteEvidence.CandidateTargetsWithCallSites}; call_sites_with_push_edx_before_lea={staticRenderIndirectIndexedCfgCallSiteEvidence.CallSitesWithPushEdxBeforeLea}; call_sites_with_lea_ecx_base_before_call={staticRenderIndirectIndexedCfgCallSiteEvidence.CallSitesWithLeaEcxBaseBeforeCall}; call_sites_with_ret_imm_4_after_call={staticRenderIndirectIndexedCfgCallSiteEvidence.CallSitesWithRetImm4AfterCall}; distinct_lea_displacements={staticRenderIndirectIndexedCfgCallSiteEvidence.DistinctLeaDisplacements}; status={staticRenderIndirectIndexedCfgCallSiteEvidence.Status}; provenance={staticRenderIndirectIndexedCfgCallSiteEvidence.Provenance}; original_semantics={staticRenderIndirectIndexedCfgCallSiteEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-indexed-cfg-call-target-cfg",
                  staticRenderIndirectIndexedCfgCallTargetCfgEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderIndirectIndexedCfgCallTargetCfgEvidence.Valid,
                  staticRenderIndirectIndexedCfgCallTargetCfgEvidence.TargetsWithReachableCfg,
                  1,
                  "bounded CFG reachability over the distinct direct-call destination observed in indexed CFGs; calls not followed",
                  "independent function-boundary and call-following evidence plus verified render-state-to-geometry/UV/material binding evidence; destination CFG remains local candidate flow",
                  $"candidate_targets={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.CandidateTargets}; analyzed_targets={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.AnalyzedTargets}; targets_with_reachable_cfg={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.TargetsWithReachableCfg}; targets_with_reachable_ret={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.TargetsWithReachableRet}; total_linear_instructions={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.TotalLinearInstructions}; total_reachable_instructions={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.TotalReachableInstructions}; total_basic_blocks={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.TotalBasicBlocks}; total_conditional_branches={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.TotalConditionalBranches}; total_unconditional_branches={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.TotalUnconditionalBranches}; total_internal_branch_targets={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.TotalInternalBranchTargets}; total_out_of_window_branch_targets={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.TotalOutOfWindowBranchTargets}; total_direct_calls={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.TotalDirectCalls}; total_unique_direct_call_targets={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.TotalUniqueDirectCallTargets}; distinct_direct_call_targets={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.DistinctDirectCallTargets}; total_indirect_control_transfers={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.TotalIndirectControlTransfers}; status={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.Status}; provenance={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.Provenance}; original_semantics={staticRenderIndirectIndexedCfgCallTargetCfgEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-indexed-cfg-nested-call-target-cfg",
                  staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.Valid,
                  staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.DistinctNestedTargets,
                  1,
                  "bounded CFG reachability over one-hop nested direct-call destinations; calls not followed",
                  "independent function-boundary and call-following evidence plus verified render-state-to-geometry/UV/material binding evidence; nested destination CFG remains local candidate flow",
                  $"parent_target_rows={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.ParentTargetRows}; parent_direct_call_occurrences={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.ParentDirectCallOccurrences}; distinct_nested_targets={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.DistinctNestedTargets}; analyzed_targets={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.AnalyzedTargets}; targets_with_reachable_cfg={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.TargetsWithReachableCfg}; targets_with_reachable_ret={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.TargetsWithReachableRet}; total_linear_instructions={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.TotalLinearInstructions}; total_reachable_instructions={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.TotalReachableInstructions}; total_basic_blocks={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.TotalBasicBlocks}; total_conditional_branches={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.TotalConditionalBranches}; total_unconditional_branches={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.TotalUnconditionalBranches}; total_internal_branch_targets={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.TotalInternalBranchTargets}; total_out_of_window_branch_targets={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.TotalOutOfWindowBranchTargets}; total_direct_calls={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.TotalDirectCalls}; total_unique_direct_call_targets={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.TotalUniqueDirectCallTargets}; distinct_nested_direct_call_targets={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.DistinctNestedDirectCallTargets}; total_indirect_control_transfers={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.TotalIndirectControlTransfers}; status={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.Status}; provenance={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.Provenance}; original_semantics={staticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-indexed-cfg-nested-call-sites",
                  staticRenderIndirectIndexedCfgNestedCallSiteEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderIndirectIndexedCfgNestedCallSiteEvidence.Valid,
                  staticRenderIndirectIndexedCfgNestedCallSiteEvidence.NestedCallTargetRows,
                  2,
                  "bounded local instruction context for calls inside the indexed CFG destination slice",
                  "independent function-boundary, pointer/object-layout and semantic evidence; nested call context remains structural only",
                  $"parent_cfg_target_rows={staticRenderIndirectIndexedCfgNestedCallSiteEvidence.ParentCfgTargetRows}; nested_call_target_rows={staticRenderIndirectIndexedCfgNestedCallSiteEvidence.NestedCallTargetRows}; distinct_nested_call_targets={staticRenderIndirectIndexedCfgNestedCallSiteEvidence.DistinctNestedCallTargets}; nested_call_sites_with_push_edx_before_lea={staticRenderIndirectIndexedCfgNestedCallSiteEvidence.NestedCallSitesWithPushEdxBeforeLea}; nested_call_sites_with_lea_ecx_base_before_call={staticRenderIndirectIndexedCfgNestedCallSiteEvidence.NestedCallSitesWithLeaEcxBaseBeforeCall}; nested_call_sites_with_ret_imm_4_after_call={staticRenderIndirectIndexedCfgNestedCallSiteEvidence.NestedCallSitesWithRetImm4AfterCall}; distinct_nested_lea_displacements={staticRenderIndirectIndexedCfgNestedCallSiteEvidence.DistinctNestedLeaDisplacements}; status={staticRenderIndirectIndexedCfgNestedCallSiteEvidence.Status}; provenance={staticRenderIndirectIndexedCfgNestedCallSiteEvidence.Provenance}; original_semantics={staticRenderIndirectIndexedCfgNestedCallSiteEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-indexed-cfg-call-site-join",
                  staticRenderIndirectIndexedCfgCallSiteJoinEvidence.Valid ? "verified-structural" : "not-present",
                  staticRenderIndirectIndexedCfgCallSiteJoinEvidence.Valid,
                  staticRenderIndirectIndexedCfgCallSiteJoinEvidence.JoinedCallSiteRows,
                  staticRenderIndirectIndexedCfgCallSiteJoinEvidence.CallSiteRows,
                  "bounded join preserving indexed-table occurrences alongside reachable CFG call-site context",
                  "independent function-boundary, pointer/object-layout and semantic evidence; join remains structural and preserves duplicate table occurrences",
                  $"indexed_table_target_rows={staticRenderIndirectIndexedCfgCallSiteJoinEvidence.IndexedTableTargetRows}; distinct_table_bases={staticRenderIndirectIndexedCfgCallSiteJoinEvidence.DistinctTableBases}; call_site_rows={staticRenderIndirectIndexedCfgCallSiteJoinEvidence.CallSiteRows}; joined_call_site_rows={staticRenderIndirectIndexedCfgCallSiteJoinEvidence.JoinedCallSiteRows}; distinct_call_targets={staticRenderIndirectIndexedCfgCallSiteJoinEvidence.DistinctCallTargets}; distinct_table_entry_indices={staticRenderIndirectIndexedCfgCallSiteJoinEvidence.DistinctTableEntryIndices}; table_entry_occurrences_for_joined_call_sites={staticRenderIndirectIndexedCfgCallSiteJoinEvidence.TableEntryOccurrencesForJoinedCallSites}; duplicate_table_occurrences_over_call_sites={staticRenderIndirectIndexedCfgCallSiteJoinEvidence.DuplicateTableOccurrencesOverCallSites}; call_sites_with_complete_local_shape={staticRenderIndirectIndexedCfgCallSiteJoinEvidence.CallSitesWithCompleteLocalShape}; status={staticRenderIndirectIndexedCfgCallSiteJoinEvidence.Status}; provenance={staticRenderIndirectIndexedCfgCallSiteJoinEvidence.Provenance}; original_semantics={staticRenderIndirectIndexedCfgCallSiteJoinEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-indexed-cfg-join",
                  staticRenderIndirectIndexedCfgJoinEvidence.Valid ? "verified-structural-negative" : "not-present",
                  staticRenderIndirectIndexedCfgJoinEvidence.Valid,
                  staticRenderIndirectIndexedCfgJoinEvidence.CfgDirectCallTargets,
                  1,
                  "bounded address-set intersection of indexed-CFG direct-call targets with render target and call-graph reports",
                  "independent pointer/function recovery and verified render-state-to-geometry/UV/material binding evidence; set non-overlap is a negative control only",
                  $"cfg_direct_call_targets={staticRenderIndirectIndexedCfgJoinEvidence.CfgDirectCallTargets}; render_direct_call_targets={staticRenderIndirectIndexedCfgJoinEvidence.RenderDirectCallTargets}; render_window_targets={staticRenderIndirectIndexedCfgJoinEvidence.RenderWindowTargets}; render_call_graph_nodes={staticRenderIndirectIndexedCfgJoinEvidence.RenderCallGraphNodes}; cfg_targets_in_render_direct_call_targets={staticRenderIndirectIndexedCfgJoinEvidence.CfgTargetsInRenderDirectCallTargets}; cfg_targets_in_render_window_targets={staticRenderIndirectIndexedCfgJoinEvidence.CfgTargetsInRenderWindowTargets}; cfg_targets_in_render_call_graph_nodes={staticRenderIndirectIndexedCfgJoinEvidence.CfgTargetsInRenderCallGraphNodes}; status={staticRenderIndirectIndexedCfgJoinEvidence.Status}; provenance={staticRenderIndirectIndexedCfgJoinEvidence.Provenance}; original_semantics={staticRenderIndirectIndexedCfgJoinEvidence.OriginalSemantics}"),
              new(
                  "rendering-static-xbe-indirect-indexed-target-join",
                  staticRenderIndirectIndexedTargetJoinEvidence.Valid ? "verified-structural-negative" : "not-present",
                  staticRenderIndirectIndexedTargetJoinEvidence.Valid,
                  staticRenderIndirectIndexedTargetJoinEvidence.CandidateTargets,
                  1,
                  "bounded address-set intersection between indexed-table candidates, direct-call targets and render-window targets",
                  "independent control-flow/function recovery and verified render-state-to-geometry/UV/material binding evidence; non-overlap is a negative control only",
                  $"candidate_targets={staticRenderIndirectIndexedTargetJoinEvidence.CandidateTargets}; direct_call_targets={staticRenderIndirectIndexedTargetJoinEvidence.DirectCallTargets}; render_window_targets={staticRenderIndirectIndexedTargetJoinEvidence.RenderWindowTargets}; candidate_targets_in_direct_call_targets={staticRenderIndirectIndexedTargetJoinEvidence.CandidateTargetsInDirectCallTargets}; candidate_targets_in_render_window_targets={staticRenderIndirectIndexedTargetJoinEvidence.CandidateTargetsInRenderWindowTargets}; status={staticRenderIndirectIndexedTargetJoinEvidence.Status}; provenance={staticRenderIndirectIndexedTargetJoinEvidence.Provenance}; original_semantics={staticRenderIndirectIndexedTargetJoinEvidence.OriginalSemantics}"),
              new(
                  "rendering-authored-scene-assets",
                authoredSceneAssetRuntimeVerified ? "verified-authored" : "not-present",
                authoredSceneAssetRuntimeVerified,
                authoredSceneAssetFrames,
                140,
                "authored managed court, goal and ball scene assets exercised by the renderer",
                "keep authored scene assets separate from original EAGL geometry, materials, UVs and GPU state",
                $"authored_scene_asset_frames={Get(integrationReport, "authored_scene_asset_frames")}; authored_scene_asset_manifest={Get(integrationReport, "authored_scene_asset_manifest")}; original_semantics={Get(integrationReport, "original_semantics")}"),
            new(
                "character-authored-asset-runtime",
                authoredCharacterRuntimeVerified ? "verified-authored" : "not-present",
                authoredCharacterRuntimeVerified,
                authoredCharacterAssetFrames,
                ReadInt(authoredCharacterIntegrationReport, "rendered_frames"),
                "authored clean-room character silhouette asset rendered without candidate mesh slots",
                "keep authored presentation separate from original EAGL mesh, material, UV, skinning, rigging and GPU semantics",
                $"rendered_frames={Get(authoredCharacterIntegrationReport, "rendered_frames")}; authored_character_asset_frames={Get(authoredCharacterIntegrationReport, "authored_character_asset_frames")}; authored_character_asset_manifest={Get(authoredCharacterIntegrationReport, "authored_character_asset_manifest")}; manifest_complete={CleanRoomCharacterAssetManifest.Default.IsComplete}; candidate_home_enabled={Get(authoredCharacterIntegrationReport, "candidate_home_enabled")}; candidate_away_enabled={Get(authoredCharacterIntegrationReport, "candidate_away_enabled")}; original_semantics={Get(authoredCharacterIntegrationReport, "original_semantics")}"),
            new(
                "character-authored-assets",
                authoredCharacterRuntimeVerified ? "verified-authored" : "not-present",
                authoredCharacterRuntimeVerified,
                authoredCharacterRuntimeVerified ? 1 : 0,
                1,
                "authored character silhouette and palette manifest resolved by the managed clean-room renderer",
                "keep authored character assets separate from original mesh, material, UV, skinning, rigging, morph and GPU semantics",
                $"authored_character_asset_manifest={Get(authoredCharacterIntegrationReport, "authored_character_asset_manifest")}; manifest_complete={CleanRoomCharacterAssetManifest.Default.IsComplete}; original_semantics={Get(authoredCharacterIntegrationReport, "original_semantics")}"),
            new(
                "gameplay-authored-runtime",
                authoredGameplayRuntimeVerified ? "verified-authored" : "not-present",
                authoredGameplayRuntimeVerified,
                authoredMatchEvents,
                1,
                "authored match simulation consumed by the integrated runtime and serialized deterministically",
                "broader authored rules coverage and independent original-rule evidence before changing the gameplay gate",
                $"match_events={authoredMatchEvents}; opponent_ai_enabled={Get(integrationReport, "opponent_ai_enabled")}; match_trace_written={Get(integrationReport, "match_trace_written")}; original_semantics={Get(integrationReport, "original_semantics")}"),
            new(
                "gameplay-authored-coverage",
                gameplayCoverageVerified ? "verified-authored" : "not-present",
                gameplayCoverageVerified,
                ReadInt(gameplayCoverageSummary, "verified_scenario_count"),
                ReadInt(gameplayCoverageSummary, "scenario_count"),
                "authored match trace/replay coverage across the implemented clean-room scenarios",
                "independent original-rule evidence before changing the gameplay gate",
                $"scenario_count={Get(gameplayCoverageSummary, "scenario_count")}; verified_scenario_count={Get(gameplayCoverageSummary, "verified_scenario_count")}; total_events={Get(gameplayCoverageSummary, "total_events")}; covered_actions={Get(gameplayCoverageSummary, "covered_actions")}; all_missing_actions={Get(gameplayCoverageSummary, "all_missing_actions")}; original_semantics={Get(gameplayCoverageSummary, "original_semantics")}"),
            new(
                "gameplay-authored-invariants",
                gameplayInvariantVerified ? "verified-authored" : "not-present",
                gameplayInvariantVerified,
                ReadInt(gameplayInvariantSummary, "verified_scenario_count"),
                ReadInt(gameplayInvariantSummary, "scenario_count"),
                "authored invariant audit over deterministic scenarios for finite state, bounds, monotonic clocks/scores, event order and end conditions",
                "independent original-rule evidence before changing the gameplay gate",
                $"scenario_count={Get(gameplayInvariantSummary, "scenario_count")}; verified_scenario_count={Get(gameplayInvariantSummary, "verified_scenario_count")}; total_frames={Get(gameplayInvariantSummary, "total_frames")}; total_events={Get(gameplayInvariantSummary, "total_events")}; original_semantics={Get(gameplayInvariantSummary, "original_semantics")}"),
            new(
                "clean-room-authored-runtime",
                authoredRuntimeVerified ? "verified-authored" : "not-present",
                authoredRuntimeVerified,
                authoredRuntimeVerified ? authoredScenarioCount : 0,
                Math.Max(1, authoredScenarioCount),
                "single authored managed-runtime gate combining gameplay traces, gameplay invariants, rendering/assets/audio integration and replay identity",
                "independent original bindings and semantics before changing any original-asset, gameplay or audio gate",
                $"gameplay_coverage={gameplayCoverageVerified}; invariant_coverage={gameplayInvariantVerified}; integrated_coverage={authoredIntegratedCoverageVerified}; audio_coverage={audioCoverageVerified}; gameplay_scenarios={Get(gameplayCoverageSummary, "scenario_count")}; invariant_scenarios={Get(gameplayInvariantSummary, "scenario_count")}; integrated_scenarios={Get(authoredIntegratedCoverageSummary, "scenario_count")}; audio_scenarios={Get(audioCoverageSummary, "scenario_count")}; original_semantics=not-claimed"),
            new(
                "audio-authored-runtime",
                authoredAudioRuntimeVerified ? "verified-authored" : "not-present",
                authoredAudioRuntimeVerified,
                authoredAudioEvents,
                1,
                "authored cue policy prepared and played by the integrated runtime",
                "independent original EVT/ABKC/sample selection evidence before changing the partially-integrated audio gate",
                $"audio_events={authoredAudioEvents}; audio_prepared={Get(integrationReport, "audio_prepared")}; audio_played={Get(integrationReport, "audio_played")}; audio_unmapped={Get(integrationReport, "audio_unmapped")}; named_event_candidate_waves_prepared={Get(integrationReport, "named_event_candidate_waves_prepared")}; named_event_candidate_pcm_bytes_prepared={Get(integrationReport, "named_event_candidate_pcm_bytes_prepared")}; named_event_candidate_decode_rejected={Get(integrationReport, "named_event_candidate_decode_rejected")}; observed_bank_candidate_count={Get(integrationReport, "observed_bank_candidate_count")}; observed_bank_candidate_decoded={Get(integrationReport, "observed_bank_candidate_decoded")}; observed_bank_candidate_rejected={Get(integrationReport, "observed_bank_candidate_rejected")}; gameplay_audio_pcm_bytes={Get(integrationReport, "gameplay_audio_pcm_bytes")}; gameplay_audio_pcm_sha256={Get(integrationReport, "gameplay_audio_pcm_sha256")}; authored_audio_asset_manifest={authoredAudioAssetManifest}; original_semantics={Get(integrationReport, "original_semantics")}"),
            new(
                "audio-candidate-runtime",
                candidateIntegratedRuntimeAuditVerified ? "verified-candidate" : "not-present",
                candidateIntegratedRuntimeAuditVerified,
                candidateIntegratedRuntimeAuditVerified ? ReadInt(candidateIntegratedRuntimeAudit, "audio_events") : 0,
                8,
                "candidate opt-in full-stack runtime prepared and played candidate audio alongside candidate render and authored gameplay",
                "independent original EVT/ABKC/sample selection evidence before changing the partially-integrated original audio gate",
                $"candidate_audio_events={Get(candidateIntegratedRuntimeAudit, "audio_events")}; audio_prepared={Get(candidateIntegratedRuntimeAudit, "audio_prepared")}; audio_played={Get(candidateIntegratedRuntimeAudit, "audio_played")}; audio_unmapped={Get(candidateIntegratedRuntimeAudit, "audio_unmapped")}; replay_matched={Get(candidateIntegratedRuntimeAudit, "replay_matched")}; original_semantics=not-claimed"),
            new(
                "audio-authored-coverage",
                audioCoverageVerified ? "verified-authored" : "not-present",
                audioCoverageVerified,
                ReadInt(audioCoverageSummary, "verified_scenario_count"),
                ReadInt(audioCoverageSummary, "scenario_count"),
                "authored gameplay-audio trace/replay coverage across the implemented clean-room scenarios",
                "independent original EVT/ABKC/sample selection evidence before changing the audio gate",
                $"scenario_count={Get(audioCoverageSummary, "scenario_count")}; verified_scenario_count={Get(audioCoverageSummary, "verified_scenario_count")}; total_events={Get(audioCoverageSummary, "total_events")}; total_prepared={Get(audioCoverageSummary, "total_prepared")}; total_played={Get(audioCoverageSummary, "total_played")}; total_unmapped={Get(audioCoverageSummary, "total_unmapped")}; covered_policy_actions={Get(audioCoverageSummary, "covered_policy_actions")}; missing_policy_actions={Get(audioCoverageSummary, "missing_policy_actions")}; original_semantics={Get(audioCoverageSummary, "original_semantics")}"),
            new(
                "audio-authored-assets",
                authoredAudioAssetRuntimeVerified ? "verified-authored" : "not-present",
                authoredAudioAssetRuntimeVerified,
                authoredAudioAssetRuntimeVerified ? 1 : 0,
                1,
                "authored audio-cue manifest validated before the managed gameplay mixer prepares a candidate wave",
                "keep authored cue assets separate from original EVT, ABKC, sample-selection and playback semantics",
                $"authored_audio_asset_manifest={authoredAudioAssetManifest}; manifest_complete={CleanRoomAudioAssetManifest.Default.IsComplete}; original_semantics={Get(integrationReport, "original_semantics")}"),
            new(
                "audio-named-event-runtime",
                namedEventRuntimeVerified ? "verified-candidate" : "not-present",
                namedEventRuntimeVerified,
                ReadInt(namedEventRuntimeSummary, "runtime_prepared"),
                namedAudio,
                "exact named EVT metadata plus parallel data range prepared by the managed runtime decoder",
                "independent gameplay event, ABKC/sample selection and original audio binding evidence",
                $"candidate_count={Get(namedEventRuntimeSummary, "candidate_count")}; candidate_metadata_validated={Get(namedEventRuntimeSummary, "candidate_metadata_validated")}; runtime_prepared={Get(namedEventRuntimeSummary, "runtime_prepared")}; wave_headers_valid={Get(namedEventRuntimeSummary, "wave_headers_valid")}; pcm_bytes={Get(namedEventRuntimeSummary, "pcm_bytes")}; status={Get(namedEventRuntimeSummary, "status")}; provenance={Get(namedEventRuntimeSummary, "provenance")}; original_semantics={Get(namedEventRuntimeSummary, "original_semantics")}"),
            new(
                "audio-event-parallel-range",
                audioParallelRangeVerified ? "verified-structural" : "not-present",
                audioParallelRangeVerified,
                ReadInt(audioParallelRangeSummary, "verified_rows"),
                namedAudio,
                "exact named EVT text anchors rejoined to parallel pcpahdr/pcpadat offset-table rows and SCHl data prefixes",
                "independent gameplay event, ABKC/sample selection and original playback semantics; names remain structural candidates",
                $"candidate_count={Get(audioParallelRangeSummary, "candidate_count")}; event_string_anchor_valid={Get(audioParallelRangeSummary, "event_string_anchor_valid")}; header_range_valid={Get(audioParallelRangeSummary, "header_range_valid")}; data_range_valid={Get(audioParallelRangeSummary, "data_range_valid")}; parallel_row_index_valid={Get(audioParallelRangeSummary, "parallel_row_index_valid")}; data_schl_prefix={Get(audioParallelRangeSummary, "data_schl_prefix")}; unique_event_names={Get(audioParallelRangeSummary, "unique_event_names")}; unique_event_string_offsets={Get(audioParallelRangeSummary, "unique_event_string_offsets")}; unique_header_rows={Get(audioParallelRangeSummary, "unique_header_rows")}; header_table_rows={Get(audioParallelRangeSummary, "header_table_rows")}; data_table_rows={Get(audioParallelRangeSummary, "data_table_rows")}; verified_rows={Get(audioParallelRangeSummary, "verified_rows")}; status={Get(audioParallelRangeSummary, "status")}; provenance={Get(audioParallelRangeSummary, "provenance")}; direct_selection_binding={Get(audioParallelRangeSummary, "direct_selection_binding")}; original_semantics={Get(audioParallelRangeSummary, "original_semantics")}"),
            new(
                  "audio-event-parallel-runtime-join-control",
                  audioParallelRuntimeJoinSummary.Valid ? "verified-structural-negative" : "not-present",
                audioParallelRuntimeJoinSummary.Valid,
                audioParallelRuntimeJoinSummary.JoinedRows,
                Math.Max(1, audioParallelRuntimeJoinSummary.ParallelRows),
                "independent join preserves exact EVT name/header/data-range identity from the parallel audit through managed named-payload preparation",
                  "does not select an ABKC sample, bind an EVT name to gameplay or claim original playback semantics",
                  $"parallel_rows={audioParallelRuntimeJoinSummary.ParallelRows}; runtime_rows={audioParallelRuntimeJoinSummary.RuntimeRows}; joined_rows={audioParallelRuntimeJoinSummary.JoinedRows}; missing_runtime_names={audioParallelRuntimeJoinSummary.MissingRuntimeNames}; missing_parallel_names={audioParallelRuntimeJoinSummary.MissingParallelNames}; parallel_unique_event_names={audioParallelRuntimeJoinSummary.ParallelUniqueEventNames}; runtime_unique_event_names={audioParallelRuntimeJoinSummary.RuntimeUniqueEventNames}; header_row_match_rows={audioParallelRuntimeJoinSummary.HeaderRowMatchRows}; data_offset_match_rows={audioParallelRuntimeJoinSummary.DataOffsetMatchRows}; data_length_match_rows={audioParallelRuntimeJoinSummary.DataLengthMatchRows}; candidate_metadata_validated_rows={audioParallelRuntimeJoinSummary.CandidateMetadataValidatedRows}; runtime_prepared_rows={audioParallelRuntimeJoinSummary.RuntimePreparedRows}; wave_header_valid_rows={audioParallelRuntimeJoinSummary.WaveHeaderValidRows}; parallel_input_status_rows={audioParallelRuntimeJoinSummary.ParallelInputStatusRows}; runtime_input_status_rows={audioParallelRuntimeJoinSummary.RuntimeInputStatusRows}; status={audioParallelRuntimeJoinSummary.Status}; provenance={audioParallelRuntimeJoinSummary.Provenance}; original_semantics={audioParallelRuntimeJoinSummary.OriginalSemantics}"),
            new(
                  "audio-event-evt-pcp-range-discrimination-control",
                  audioEventEvtPcpRangeEvidence.Valid ? "verified-structural-negative" : "not-present",
                  audioEventEvtPcpRangeEvidence.Valid,
                  audioEventEvtPcpRangeEvidence.ValidNamedRangeRows,
                  Math.Max(1, audioEventEvtPcpRangeEvidence.ParallelRows),
                  "independent EVT-name reparse reconciles unique pcpahdr/pcpadat ranges and SCHl prefixes while preserving the separate 32-bit raw-word negative control",
                  "does not select an ABKC/AST sample, establish gameplay-event selection or claim original playback semantics",
                  $"parallel_rows={audioEventEvtPcpRangeEvidence.ParallelRows}; valid_named_range_rows={audioEventEvtPcpRangeEvidence.ValidNamedRangeRows}; event_string_anchor_rows={audioEventEvtPcpRangeEvidence.EventStringAnchorRows}; header_range_rows={audioEventEvtPcpRangeEvidence.HeaderRangeRows}; data_range_rows={audioEventEvtPcpRangeEvidence.DataRangeRows}; parallel_row_index_rows={audioEventEvtPcpRangeEvidence.ParallelRowIndexRows}; data_schl_rows={audioEventEvtPcpRangeEvidence.DataSchlRows}; unique_event_name_rows={audioEventEvtPcpRangeEvidence.UniqueEventNameRows}; unique_event_string_offset_rows={audioEventEvtPcpRangeEvidence.UniqueEventStringOffsetRows}; unique_header_row_rows={audioEventEvtPcpRangeEvidence.UniqueHeaderRowRows}; parallel_status_valid_rows={audioEventEvtPcpRangeEvidence.ParallelStatusValidRows}; index_candidate_names_contains_event_rows={audioEventEvtPcpRangeEvidence.IndexCandidateNamesContainsEventRows}; index_32bit_abk_zero={audioEventEvtPcpRangeEvidence.Index32BitAbkZero}; index_32bit_pcpahdr_zero={audioEventEvtPcpRangeEvidence.Index32BitPcpahdrZero}; index_32bit_pcpadat_zero={audioEventEvtPcpRangeEvidence.Index32BitPcpadatZero}; runtime_joined_rows={audioEventEvtPcpRangeEvidence.RuntimeJoinedRows}; runtime_missing_names={audioEventEvtPcpRangeEvidence.RuntimeMissingNames}; status={audioEventEvtPcpRangeEvidence.Status}; provenance={audioEventEvtPcpRangeEvidence.Provenance}; direct_selection_binding={audioEventEvtPcpRangeEvidence.DirectSelectionBinding}; original_semantics={audioEventEvtPcpRangeEvidence.OriginalSemantics}"),
            new(
                  "audio-event-audio-asset-hash-universe-control",
                  audioEventAudioAssetHashUniverseEvidence.Valid ? "verified-structural-negative" : "not-present",
                  audioEventAudioAssetHashUniverseEvidence.Valid,
                  audioEventAudioAssetHashUniverseEvidence.ValidAssetCount,
                  Math.Max(1, audioEventAudioAssetHashUniverseEvidence.ScannedAssetCount),
                  "independent aligned-word reparse of every data/audio asset except the source eventspa.evt",
                  "does not select an ABKC/AST sample, establish gameplay-event selection or claim original playback semantics",
                  $"event_path=data/audio/eventspa.evt; candidate_count={audioEventAudioAssetHashUniverseEvidence.CandidateCount}; scanned_asset_count={audioEventAudioAssetHashUniverseEvidence.ScannedAssetCount}; valid_asset_count={audioEventAudioAssetHashUniverseEvidence.ValidAssetCount}; scanned_bytes={audioEventAudioAssetHashUniverseEvidence.ScannedBytes}; excluded_source_rows={audioEventAudioAssetHashUniverseEvidence.ExcludedSourceRows}; assets_with_hits={audioEventAudioAssetHashUniverseEvidence.AssetsWithHits}; asset_hash_hit_pairs={audioEventAudioAssetHashUniverseEvidence.AssetHashHitPairs}; aligned_occurrences={audioEventAudioAssetHashUniverseEvidence.AlignedOccurrences}; status={audioEventAudioAssetHashUniverseEvidence.Status}; provenance={audioEventAudioAssetHashUniverseEvidence.Provenance}; direct_selection_binding={audioEventAudioAssetHashUniverseEvidence.DirectSelectionBinding}; original_semantics={audioEventAudioAssetHashUniverseEvidence.OriginalSemantics}"),
            new(
                  "audio-event-data-asset-hash-universe-control",
                  audioEventDataAssetHashUniverseEvidence.Valid
                      ? (audioEventDataAssetHashUniverseEvidence.AssetsWithHits == 0 &&
                         audioEventDataAssetHashUniverseEvidence.AssetHashHitPairs == 0 &&
                         audioEventDataAssetHashUniverseEvidence.AlignedOccurrences == 0
                            ? "verified-structural-negative"
                            : "verified-structural")
                      : "not-present",
                  audioEventDataAssetHashUniverseEvidence.Valid,
                  audioEventDataAssetHashUniverseEvidence.ValidAssetCount,
                  Math.Max(1, audioEventDataAssetHashUniverseEvidence.ScannedAssetCount),
                  "independent aligned-word reparse of every data asset except the source eventspa.evt",
                  "does not select an ABKC/AST sample, establish gameplay-event selection or claim original playback semantics",
                  $"asset_root={audioEventDataAssetHashUniverseEvidence.AssetRoot}; event_path={audioEventDataAssetHashUniverseEvidence.EventPath}; candidate_count={audioEventDataAssetHashUniverseEvidence.CandidateCount}; scanned_asset_count={audioEventDataAssetHashUniverseEvidence.ScannedAssetCount}; valid_asset_count={audioEventDataAssetHashUniverseEvidence.ValidAssetCount}; scanned_bytes={audioEventDataAssetHashUniverseEvidence.ScannedBytes}; excluded_source_rows={audioEventDataAssetHashUniverseEvidence.ExcludedSourceRows}; assets_with_hits={audioEventDataAssetHashUniverseEvidence.AssetsWithHits}; asset_hash_hit_pairs={audioEventDataAssetHashUniverseEvidence.AssetHashHitPairs}; aligned_occurrences={audioEventDataAssetHashUniverseEvidence.AlignedOccurrences}; status={audioEventDataAssetHashUniverseEvidence.Status}; provenance={audioEventDataAssetHashUniverseEvidence.Provenance}; direct_selection_binding={audioEventDataAssetHashUniverseEvidence.DirectSelectionBinding}; original_semantics={audioEventDataAssetHashUniverseEvidence.OriginalSemantics}"),
            new(
                  "audio-event-data-asset-hash-universe-independent-reparse",
                  audioEventDataAssetHashUniverseControlEvidence.Valid
                      ? (audioEventDataAssetHashUniverseControlEvidence.AssetsWithHits == 0 &&
                         audioEventDataAssetHashUniverseControlEvidence.AssetHashHitPairs == 0 &&
                         audioEventDataAssetHashUniverseControlEvidence.AlignedOccurrences == 0
                            ? "verified-structural-negative"
                            : "verified-structural")
                      : "not-present",
                  audioEventDataAssetHashUniverseControlEvidence.Valid,
                  audioEventDataAssetHashUniverseControlEvidence.ValidRows,
                  Math.Max(1, audioEventDataAssetHashUniverseControlEvidence.Rows),
                  "independent reparse of the complete data-universe TSV, including the exact hash/name/path/offset hit",
                  "does not establish that a compressed movie payload is an audio asset or that the EVT name is selected for playback",
                  $"asset_root={audioEventDataAssetHashUniverseControlEvidence.AssetRoot}; event_path={audioEventDataAssetHashUniverseControlEvidence.EventPath}; candidate_count={audioEventDataAssetHashUniverseControlEvidence.CandidateCount}; rows={audioEventDataAssetHashUniverseControlEvidence.Rows}; valid_rows={audioEventDataAssetHashUniverseControlEvidence.ValidRows}; unique_paths={audioEventDataAssetHashUniverseControlEvidence.UniquePaths}; scanned_bytes={audioEventDataAssetHashUniverseControlEvidence.ScannedBytes}; recomputed_bytes={audioEventDataAssetHashUniverseControlEvidence.RecomputedBytes}; excluded_source_rows={audioEventDataAssetHashUniverseControlEvidence.ExcludedSourceRows}; assets_with_hits={audioEventDataAssetHashUniverseControlEvidence.AssetsWithHits}; hit_rows={audioEventDataAssetHashUniverseControlEvidence.HitRows}; asset_hash_hit_pairs={audioEventDataAssetHashUniverseControlEvidence.AssetHashHitPairs}; aligned_occurrences={audioEventDataAssetHashUniverseControlEvidence.AlignedOccurrences}; hit_asset_path={audioEventDataAssetHashUniverseControlEvidence.HitAssetPath}; hit_hash_word={audioEventDataAssetHashUniverseControlEvidence.HitHashWord}; hit_event_name={audioEventDataAssetHashUniverseControlEvidence.HitEventName}; hit_offset={audioEventDataAssetHashUniverseControlEvidence.HitOffset}; status={audioEventDataAssetHashUniverseControlEvidence.Status}; provenance={audioEventDataAssetHashUniverseControlEvidence.Provenance}; direct_selection_binding={audioEventDataAssetHashUniverseControlEvidence.DirectSelectionBinding}; original_semantics={audioEventDataAssetHashUniverseControlEvidence.OriginalSemantics}"),
            new(
                  "audio-event-name-asset-text-search-control",
                  audioEventNameTextEvidence.Valid ? "verified-structural-negative" : "not-present",
                  audioEventNameTextEvidence.Valid,
                  audioEventNameTextEvidence.ValidAssetCount,
                  Math.Max(1, audioEventNameTextEvidence.ScannedAssetCount),
                  "exact ASCII search of every named EVT candidate across every data/audio asset except the source eventspa.evt",
                  "does not select an ABKC/AST/BNK sample, establish gameplay-event selection or claim original playback semantics",
                  $"candidate_count={audioEventNameTextEvidence.CandidateCount}; scanned_asset_count={audioEventNameTextEvidence.ScannedAssetCount}; valid_asset_count={audioEventNameTextEvidence.ValidAssetCount}; scanned_bytes={audioEventNameTextEvidence.ScannedBytes}; excluded_source_rows={audioEventNameTextEvidence.ExcludedSourceRows}; asset_name_hit_pairs={audioEventNameTextEvidence.AssetNameHitPairs}; assets_with_name_hits={audioEventNameTextEvidence.AssetsWithNameHits}; raw_occurrences={audioEventNameTextEvidence.RawOccurrences}; status={audioEventNameTextEvidence.Status}; provenance={audioEventNameTextEvidence.Provenance}; direct_selection_binding={audioEventNameTextEvidence.DirectSelectionBinding}; original_semantics={audioEventNameTextEvidence.OriginalSemantics}"),
            new(
                  "audio-event-name-asset-text-search-independent-reparse",
                  audioEventNameTextControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                  audioEventNameTextControlEvidence.Valid,
                  audioEventNameTextControlEvidence.ValidRows,
                  Math.Max(1, audioEventNameTextControlEvidence.InputRows),
                  "independent reparse of the complete audio-asset/name matrix and zero-hit reconciliation",
                  "does not select an ABKC/AST/BNK sample, establish gameplay-event selection or claim original playback semantics",
                  $"input_rows={audioEventNameTextControlEvidence.InputRows}; distinct_assets={audioEventNameTextControlEvidence.DistinctAssets}; distinct_names={audioEventNameTextControlEvidence.DistinctNames}; valid_rows={audioEventNameTextControlEvidence.ValidRows}; no_hit_rows={audioEventNameTextControlEvidence.NoHitRows}; raw_occurrence_rows={audioEventNameTextControlEvidence.RawOccurrenceRows}; total_raw_occurrences={audioEventNameTextControlEvidence.TotalRawOccurrences}; duplicate_asset_name_keys={audioEventNameTextControlEvidence.DuplicateAssetNameKeys}; source_summary_matches_recomputed={audioEventNameTextControlEvidence.SourceSummaryMatchesRecomputed}; expected_assets={audioEventNameTextControlEvidence.ExpectedAssets}; expected_names={audioEventNameTextControlEvidence.ExpectedNames}; status={audioEventNameTextControlEvidence.Status}; semantic_binding={audioEventNameTextControlEvidence.SemanticBinding}; provenance={audioEventNameTextControlEvidence.Provenance}; direct_selection_binding={audioEventNameTextControlEvidence.DirectSelectionBinding}; original_semantics={audioEventNameTextControlEvidence.OriginalSemantics}"),
            new(
                "audio-event-abk-raw-identity-control",
                audioEventAbkRawIdentityEvidence.Valid ? "verified-structural-negative" : "not-present",
                audioEventAbkRawIdentityEvidence.Valid,
                audioEventAbkRawIdentityEvidence.EventRawRows,
                Math.Max(1, audioEventAbkRawIdentityEvidence.EventCandidates),
                "bounded raw SHA-256 rejoin of all named EVT data ranges against the validated ABKC/AST SCHl range population",
                "does not select an ABKC sample, bind an EVT name to gameplay or claim original playback semantics; raw-prefix observations remain non-discriminative",
                $"event_candidates={audioEventAbkRawIdentityEvidence.EventCandidates}; event_raw_rows={audioEventAbkRawIdentityEvidence.EventRawRows}; event_distinct_raw_hashes={audioEventAbkRawIdentityEvidence.EventDistinctRawHashes}; bank_count={audioEventAbkRawIdentityEvidence.BankCount}; bank_non_dummy_sound_entries={audioEventAbkRawIdentityEvidence.BankNonDummySoundEntries}; bank_ast_range_rows={audioEventAbkRawIdentityEvidence.BankAstRangeRows}; bank_ast_schl_rows={audioEventAbkRawIdentityEvidence.BankAstSchlRows}; full_raw_hash_pairs={audioEventAbkRawIdentityEvidence.FullRawHashPairs}; event_hashes_with_full_raw_match={audioEventAbkRawIdentityEvidence.EventHashesWithFullRawMatch}; raw_prefix_pairs={audioEventAbkRawIdentityEvidence.RawPrefixPairs}; rejected_rows={audioEventAbkRawIdentityEvidence.RejectedRows}; status={audioEventAbkRawIdentityEvidence.Status}; provenance={audioEventAbkRawIdentityEvidence.Provenance}; control_status={audioEventAbkRawIdentityEvidence.ControlStatus}; original_semantics={audioEventAbkRawIdentityEvidence.OriginalSemantics}"),
            new(
                "audio-authored-candidate-route-control",
                authoredAudioCandidateRouteEvidence.Valid ? "verified-authored" : "not-present",
                authoredAudioCandidateRouteEvidence.Valid,
                authoredAudioCandidateRouteEvidence.RouteRows,
                Math.Max(1, authoredAudioCandidateRouteEvidence.RouteRows),
                "independent reparse of authored audio traces joined action/candidate routes to prepared named EVT runtime candidates",
                "does not establish original event IDs, ABKC/sample selection, gameplay semantics or original playback; authored policy remains clean-room",
                $"scenario_rows={authoredAudioCandidateRouteEvidence.ScenarioRows}; verified_scenario_rows={authoredAudioCandidateRouteEvidence.VerifiedScenarioRows}; trace_rows={authoredAudioCandidateRouteEvidence.TraceRows}; trace_played_rows={authoredAudioCandidateRouteEvidence.TracePlayedRows}; route_rows={authoredAudioCandidateRouteEvidence.RouteRows}; unique_action_rows={authoredAudioCandidateRouteEvidence.UniqueActionRows}; unique_candidate_rows={authoredAudioCandidateRouteEvidence.UniqueCandidateRows}; runtime_rows={authoredAudioCandidateRouteEvidence.RuntimeRows}; runtime_prepared_route_rows={authoredAudioCandidateRouteEvidence.RuntimePreparedRouteRows}; runtime_wave_header_valid_route_rows={authoredAudioCandidateRouteEvidence.RuntimeWaveHeaderValidRouteRows}; missing_runtime_candidates={authoredAudioCandidateRouteEvidence.MissingRuntimeCandidates}; coverage_missing_policy_actions={authoredAudioCandidateRouteEvidence.CoverageMissingPolicyActions}; status={authoredAudioCandidateRouteEvidence.Status}; provenance={authoredAudioCandidateRouteEvidence.Provenance}; original_semantics={authoredAudioCandidateRouteEvidence.OriginalSemantics}"),
            new(
                "audio-authored-named-payload-identity-control",
                authoredNamedPayloadIdentityEvidence.Valid ? "verified-authored" : "not-present",
                authoredNamedPayloadIdentityEvidence.Valid,
                authoredNamedPayloadIdentityEvidence.PcmSha256MatchRows,
                Math.Max(1, authoredNamedPayloadIdentityEvidence.IdentityRows),
                "independent reparse reconciles every authored gameplay audio trace WAVE/PCM identity with its named EVT runtime candidate",
                "does not establish original event IDs, ABKC/sample selection, gameplay semantics or original playback; the route remains clean-room candidate policy",
                $"scenario_rows={authoredNamedPayloadIdentityEvidence.ScenarioRows}; verified_scenario_rows={authoredNamedPayloadIdentityEvidence.VerifiedScenarioRows}; trace_rows={authoredNamedPayloadIdentityEvidence.TraceRows}; trace_played_rows={authoredNamedPayloadIdentityEvidence.TracePlayedRows}; identity_rows={authoredNamedPayloadIdentityEvidence.IdentityRows}; runtime_rows={authoredNamedPayloadIdentityEvidence.RuntimeRows}; runtime_prepared_route_rows={authoredNamedPayloadIdentityEvidence.RuntimePreparedRouteRows}; runtime_wave_header_valid_route_rows={authoredNamedPayloadIdentityEvidence.RuntimeWaveHeaderValidRouteRows}; wave_bytes_match_rows={authoredNamedPayloadIdentityEvidence.WaveBytesMatchRows}; pcm_bytes_match_rows={authoredNamedPayloadIdentityEvidence.PcmBytesMatchRows}; pcm_sha256_match_rows={authoredNamedPayloadIdentityEvidence.PcmSha256MatchRows}; invalid_trace_identity_rows={authoredNamedPayloadIdentityEvidence.InvalidTraceIdentityRows}; missing_runtime_candidates={authoredNamedPayloadIdentityEvidence.MissingRuntimeCandidates}; status={authoredNamedPayloadIdentityEvidence.Status}; provenance={authoredNamedPayloadIdentityEvidence.Provenance}; direct_selection_binding={authoredNamedPayloadIdentityEvidence.DirectSelectionBinding}; original_semantics={authoredNamedPayloadIdentityEvidence.OriginalSemantics}"),
            new(
                "audio-event-selection-negative-synthesis",
                audioSelectionNegativeSynthesisVerified ? "verified-structural-negative" : "not-present",
                audioSelectionNegativeSynthesisVerified,
                audioSelectionNegativeSynthesisVerified ? ReadInt(audioSelectionNegativeSynthesisSummary, "valid_source_count") : 0,
                Math.Max(1, ReadInt(audioSelectionNegativeSynthesisSummary, "source_count")),
                "independent synthesis of EVT index, XBE word-search, exhaustive payload PCM, named-range join and authored-route controls",
                "an explicit EVT→ABKC/sample/playback binding and original event semantics; a negative synthesis is not a decoder",
                $"source_count={Get(audioSelectionNegativeSynthesisSummary, "source_count")}; valid_source_count={Get(audioSelectionNegativeSynthesisSummary, "valid_source_count")}; index_candidate_count={Get(audioSelectionNegativeSynthesisSummary, "index_candidate_count")}; index_abk_occurrences={Get(audioSelectionNegativeSynthesisSummary, "index_abk_occurrences")}; index_header_occurrences={Get(audioSelectionNegativeSynthesisSummary, "index_header_occurrences")}; index_data_occurrences={Get(audioSelectionNegativeSynthesisSummary, "index_data_occurrences")}; xbe_candidate_count={Get(audioSelectionNegativeSynthesisSummary, "xbe_candidate_count")}; xbe_aligned_occurrences={Get(audioSelectionNegativeSynthesisSummary, "xbe_aligned_occurrences")}; xbe_unaligned_occurrences={Get(audioSelectionNegativeSynthesisSummary, "xbe_unaligned_occurrences")}; payload_event_candidates={Get(audioSelectionNegativeSynthesisSummary, "payload_event_candidates")}; payload_event_decoded={Get(audioSelectionNegativeSynthesisSummary, "payload_event_decoded")}; payload_bank_count={Get(audioSelectionNegativeSynthesisSummary, "payload_bank_count")}; payload_bank_sound_candidates={Get(audioSelectionNegativeSynthesisSummary, "payload_bank_sound_candidates")}; payload_bank_sound_decoded={Get(audioSelectionNegativeSynthesisSummary, "payload_bank_sound_decoded")}; payload_exact_pcm_hash_pairs={Get(audioSelectionNegativeSynthesisSummary, "payload_exact_pcm_hash_pairs")}; payload_event_hashes_with_match={Get(audioSelectionNegativeSynthesisSummary, "payload_event_hashes_with_match")}; parallel_joined_rows={Get(audioSelectionNegativeSynthesisSummary, "parallel_joined_rows")}; authored_route_rows={Get(audioSelectionNegativeSynthesisSummary, "authored_route_rows")}; status={Get(audioSelectionNegativeSynthesisSummary, "status")}; provenance={Get(audioSelectionNegativeSynthesisSummary, "provenance")}; direct_selection_binding={Get(audioSelectionNegativeSynthesisSummary, "direct_selection_binding")}; original_semantics={Get(audioSelectionNegativeSynthesisSummary, "original_semantics")}"),
            new(
                "audio-event-xbe-hash-search",
                audioXbeHashSearchVerified ? "verified-structural" : "not-present",
                audioXbeHashSearchVerified,
                ReadInt(audioXbeHashSearchSummary, "candidate_count"),
                namedAudio,
                "bounded aligned and byte-search of EVT string-index words in default.xbe",
                "raw XBE word occurrences do not recover functions, event IDs, ABKC samples or playback semantics",
                $"candidate_count={Get(audioXbeHashSearchSummary, "candidate_count")}; unique_event_names={Get(audioXbeHashSearchSummary, "unique_event_names")}; unique_hash_words={Get(audioXbeHashSearchSummary, "unique_hash_words")}; xbe_bytes={Get(audioXbeHashSearchSummary, "xbe_bytes")}; aligned_occurrences={Get(audioXbeHashSearchSummary, "aligned_occurrences")}; unaligned_occurrences={Get(audioXbeHashSearchSummary, "unaligned_occurrences")}; hash_words_with_aligned_hits={Get(audioXbeHashSearchSummary, "hash_words_with_aligned_hits")}; hash_words_with_unaligned_hits={Get(audioXbeHashSearchSummary, "hash_words_with_unaligned_hits")}; status={Get(audioXbeHashSearchSummary, "status")}; provenance={Get(audioXbeHashSearchSummary, "provenance")}; direct_selection_binding={Get(audioXbeHashSearchSummary, "direct_selection_binding")}; original_semantics={Get(audioXbeHashSearchSummary, "original_semantics")}"),
            new(
                "audio-fifast2-csi-tuna-hash-crosscheck",
                fifast2CsiTunaVerified ? "verified-structural" : "not-present",
                fifast2CsiTunaVerified,
                fifast2CsiTunaVerified ? ReadInt(fifast2CsiTunaSummary, "csi_rows_with_tuna_hash_match") : 0,
                Math.Max(1, ReadInt(fifast2CsiTunaSummary, "csi_record_count")),
                "independent tuna/tunavars name-hash cross-check against the 100-record FIFAST2.csi namespace",
                "an explicit CSI opaque-word meaning, EVT→ABKC/sample selection and original playback semantics",
                $"tuna_rows={Get(fifast2CsiTunaSummary, "tuna_row_count")}; tuna_hash_matches={Get(fifast2CsiTunaSummary, "tuna_hash_match_count")}; csi_records={Get(fifast2CsiTunaSummary, "csi_record_count")}; csi_valid_records={Get(fifast2CsiTunaSummary, "csi_valid_record_count")}; csi_name_overlap={Get(fifast2CsiTunaSummary, "csi_name_overlap_count")}; csi_rows_with_tuna_hash_match={Get(fifast2CsiTunaSummary, "csi_rows_with_tuna_hash_match")}; csi_opaque_hash_matches={Get(fifast2CsiTunaSummary, "csi_opaque_hash_match_count")}; hash_function_candidate={Get(fifast2CsiTunaSummary, "hash_function_candidate")}; opaque_word_semantics={Get(fifast2CsiTunaSummary, "opaque_word_semantics")}; status={Get(fifast2CsiTunaSummary, "status")}; provenance={Get(fifast2CsiTunaSummary, "provenance")}; direct_selection_binding={Get(fifast2CsiTunaSummary, "direct_selection_binding")}; original_semantics={Get(fifast2CsiTunaSummary, "original_semantics")}"),
            new(
                "audio-fifast2-csi-evt-name-ambiguity-control",
                fifast2CsiEvtNameAmbiguityEvidence.Valid ? "verified-structural-negative" : "not-present",
                fifast2CsiEvtNameAmbiguityEvidence.Valid,
                fifast2CsiEvtNameAmbiguityEvidence.EvtValidRows,
                Math.Max(1, fifast2CsiEvtNameAmbiguityEvidence.EvtRows),
                "independent exact-token reparse compares the 100 FIFAST2.csi names with the 17 named EVT candidates",
                "does not establish a CSI event meaning, EVT→ABKC/sample selection, gameplay binding or original playback; textual overlap remains ambiguous",
                $"csi_rows={fifast2CsiEvtNameAmbiguityEvidence.CsiRows}; evt_rows={fifast2CsiEvtNameAmbiguityEvidence.EvtRows}; csi_valid_rows={fifast2CsiEvtNameAmbiguityEvidence.CsiValidRows}; evt_valid_rows={fifast2CsiEvtNameAmbiguityEvidence.EvtValidRows}; evt_unique_names={fifast2CsiEvtNameAmbiguityEvidence.EvtUniqueNames}; evt_rows_with_zero_csi_name_matches={fifast2CsiEvtNameAmbiguityEvidence.EvtRowsWithZeroCsiNameMatches}; evt_rows_with_single_csi_name_match={fifast2CsiEvtNameAmbiguityEvidence.EvtRowsWithSingleCsiNameMatch}; evt_rows_with_ambiguous_csi_name_matches={fifast2CsiEvtNameAmbiguityEvidence.EvtRowsWithAmbiguousCsiNameMatches}; evt_rows_with_any_csi_name_match={fifast2CsiEvtNameAmbiguityEvidence.EvtRowsWithAnyCsiNameMatch}; exact_event_name_overlap_rows={fifast2CsiEvtNameAmbiguityEvidence.ExactEventNameOverlapRows}; unique_name_binding_proven_rows={fifast2CsiEvtNameAmbiguityEvidence.UniqueNameBindingProvenRows}; status={fifast2CsiEvtNameAmbiguityEvidence.Status}; semantic_binding={fifast2CsiEvtNameAmbiguityEvidence.SemanticBinding}; direct_selection_binding={fifast2CsiEvtNameAmbiguityEvidence.DirectSelectionBinding}; provenance={fifast2CsiEvtNameAmbiguityEvidence.Provenance}; original_semantics={fifast2CsiEvtNameAmbiguityEvidence.OriginalSemantics}"),
            new(
                "audio-event-sample-global-assignment-control",
                audioEventSampleGlobalAssignmentEvidence.Valid ? "verified-structural-negative" : "not-present",
                audioEventSampleGlobalAssignmentEvidence.Valid,
                audioEventSampleGlobalAssignmentEvidence.ValidEventRows,
                Math.Max(1, audioEventSampleGlobalAssignmentEvidence.EventRows),
                "independent global reparse enumerates exact EVT PCM identity, weak CSI-name domains and the decoded bank pool without assigning an EVT to an ABKC sample",
                "an original EVT→ABKC/AST/BNK sample selector, gameplay event binding or playback semantics; the 241-row bank pool remains explicitly unconstrained",
                $"event_rows={audioEventSampleGlobalAssignmentEvidence.EventRows}; bank_rows={audioEventSampleGlobalAssignmentEvidence.BankRows}; decoded_event_rows={audioEventSampleGlobalAssignmentEvidence.DecodedEventRows}; decoded_bank_rows={audioEventSampleGlobalAssignmentEvidence.DecodedBankRows}; event_input_status_valid_rows={audioEventSampleGlobalAssignmentEvidence.EventInputStatusValidRows}; csi_input_status_valid_rows={audioEventSampleGlobalAssignmentEvidence.CsiInputStatusValidRows}; parallel_input_status_valid_rows={audioEventSampleGlobalAssignmentEvidence.ParallelInputStatusValidRows}; negative_synthesis_valid_source_count={audioEventSampleGlobalAssignmentEvidence.NegativeSynthesisValidSourceCount}; authored_route_rows={audioEventSampleGlobalAssignmentEvidence.AuthoredRouteRows}; bank_candidate_pool_size={audioEventSampleGlobalAssignmentEvidence.BankCandidatePoolSize}; event_exact_pcm_match_rows={audioEventSampleGlobalAssignmentEvidence.EventExactPcmMatchRows}; event_hashes_with_match={audioEventSampleGlobalAssignmentEvidence.EventHashesWithMatch}; evt_rows_with_zero_exact_sample_models={audioEventSampleGlobalAssignmentEvidence.EventRowsWithZeroExactSampleModels}; evt_rows_with_single_weak_name_models={audioEventSampleGlobalAssignmentEvidence.EventRowsWithSingleWeakNameModels}; evt_rows_with_multiple_weak_name_models={audioEventSampleGlobalAssignmentEvidence.EventRowsWithMultipleWeakNameModels}; evt_rows_with_zero_weak_name_models={audioEventSampleGlobalAssignmentEvidence.EventRowsWithZeroWeakNameModels}; weak_name_domain_sum={audioEventSampleGlobalAssignmentEvidence.WeakNameDomainSum}; exact_identity_model_sum={audioEventSampleGlobalAssignmentEvidence.ExactIdentityModelSum}; structural_cross_product_model_sum={audioEventSampleGlobalAssignmentEvidence.StructuralCrossProductModelSum}; unique_evt_abkc_sample_binding_proven_rows={audioEventSampleGlobalAssignmentEvidence.UniqueEvtAbkcSampleBindingProvenRows}; valid_event_rows={audioEventSampleGlobalAssignmentEvidence.ValidEventRows}; status={audioEventSampleGlobalAssignmentEvidence.Status}; semantic_binding={audioEventSampleGlobalAssignmentEvidence.SemanticBinding}; provenance={audioEventSampleGlobalAssignmentEvidence.Provenance}; direct_selection_binding={audioEventSampleGlobalAssignmentEvidence.DirectSelectionBinding}; original_semantics={audioEventSampleGlobalAssignmentEvidence.OriginalSemantics}"),
            new(
                "audio-fifast2-csi-opaque-range-boundary-control",
                fifast2CsiOpaqueRangeVerified ? "verified-structural-negative" : "not-present",
                fifast2CsiOpaqueRangeVerified,
                fifast2CsiOpaqueRangeVerified ? ReadInt(fifast2CsiOpaqueRangeSummary, "csi_record_count") : 0,
                Math.Max(1, ReadInt(fifast2CsiOpaqueRangeSummary, "csi_record_count")),
                "independent comparison of CSI opaque words against complete pcpahdr/pcpadat .off partitions and exact boundaries",
                "an opaque-word meaning, header/data subfield identity, EVT→ABKC/sample selection or original playback",
                $"pcpahdr_rows={Get(fifast2CsiOpaqueRangeSummary, "pcpahdr_row_count")}; pcpahdr_partition={Get(fifast2CsiOpaqueRangeSummary, "pcpahdr_partition_coverage")}; pcpahdr_within={Get(fifast2CsiOpaqueRangeSummary, "csi_opaque_within_pcpahdr_count")}; pcpahdr_exact_start={Get(fifast2CsiOpaqueRangeSummary, "csi_opaque_pcpahdr_exact_start_count")}; pcpahdr_exact_end={Get(fifast2CsiOpaqueRangeSummary, "csi_opaque_pcpahdr_exact_end_count")}; pcpadat_rows={Get(fifast2CsiOpaqueRangeSummary, "pcpadat_row_count")}; pcpadat_partition={Get(fifast2CsiOpaqueRangeSummary, "pcpadat_partition_coverage")}; pcpadat_within={Get(fifast2CsiOpaqueRangeSummary, "csi_opaque_within_pcpadat_count")}; pcpadat_exact_start={Get(fifast2CsiOpaqueRangeSummary, "csi_opaque_pcpadat_exact_start_count")}; pcpadat_exact_end={Get(fifast2CsiOpaqueRangeSummary, "csi_opaque_pcpadat_exact_end_count")}; distinct_pcpahdr_rows={Get(fifast2CsiOpaqueRangeSummary, "distinct_pcpahdr_containing_rows")}; distinct_pcpadat_rows={Get(fifast2CsiOpaqueRangeSummary, "distinct_pcpadat_containing_rows")}; opaque_word_semantics={Get(fifast2CsiOpaqueRangeSummary, "opaque_word_semantics")}; status={Get(fifast2CsiOpaqueRangeSummary, "status")}; provenance={Get(fifast2CsiOpaqueRangeSummary, "provenance")}; direct_selection_binding={Get(fifast2CsiOpaqueRangeSummary, "direct_selection_binding")}; original_semantics={Get(fifast2CsiOpaqueRangeSummary, "original_semantics")}"),
            new(
                "audio-fifast2-csi-opaque-off-field-control",
                fifast2CsiOpaqueFieldVerified ? "verified-structural-negative" : "not-present",
                fifast2CsiOpaqueFieldVerified,
                fifast2CsiOpaqueFieldVerified ? ReadInt(fifast2CsiOpaqueFieldSummary, "csi_record_count") : 0,
                Math.Max(1, ReadInt(fifast2CsiOpaqueFieldSummary, "csi_record_count")),
                "independent positional equality control against all numeric columns and row ordinals of the complete pcpahdr/pcpadat .off tables",
                "an opaque-word meaning, header/data subfield identity, EVT→ABKC/sample selection or original playback",
                $"pcpahdr_rows={Get(fifast2CsiOpaqueFieldSummary, "pcpahdr_row_count")}; pcpahdr_partition={Get(fifast2CsiOpaqueFieldSummary, "pcpahdr_partition_coverage")}; pcpahdr_any_column={Get(fifast2CsiOpaqueFieldSummary, "pcpahdr_any_column_match_count")}; pcpahdr_column1={Get(fifast2CsiOpaqueFieldSummary, "pcpahdr_column1_match_count")}; pcpahdr_column2={Get(fifast2CsiOpaqueFieldSummary, "pcpahdr_column2_match_count")}; pcpahdr_column3={Get(fifast2CsiOpaqueFieldSummary, "pcpahdr_column3_match_count")}; pcpahdr_row_ordinal={Get(fifast2CsiOpaqueFieldSummary, "pcpahdr_row_ordinal_match_count")}; pcpadat_rows={Get(fifast2CsiOpaqueFieldSummary, "pcpadat_row_count")}; pcpadat_partition={Get(fifast2CsiOpaqueFieldSummary, "pcpadat_partition_coverage")}; pcpadat_any_column={Get(fifast2CsiOpaqueFieldSummary, "pcpadat_any_column_match_count")}; pcpadat_column1={Get(fifast2CsiOpaqueFieldSummary, "pcpadat_column1_match_count")}; pcpadat_column2={Get(fifast2CsiOpaqueFieldSummary, "pcpadat_column2_match_count")}; pcpadat_column3={Get(fifast2CsiOpaqueFieldSummary, "pcpadat_column3_match_count")}; pcpadat_row_ordinal={Get(fifast2CsiOpaqueFieldSummary, "pcpadat_row_ordinal_match_count")}; opaque_word_semantics={Get(fifast2CsiOpaqueFieldSummary, "opaque_word_semantics")}; status={Get(fifast2CsiOpaqueFieldSummary, "status")}; provenance={Get(fifast2CsiOpaqueFieldSummary, "provenance")}; direct_selection_binding={Get(fifast2CsiOpaqueFieldSummary, "direct_selection_binding")}; original_semantics={Get(fifast2CsiOpaqueFieldSummary, "original_semantics")}"),
            new(
                "audio-event-payload-policy-control",
                audioPayloadPolicyControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                audioPayloadPolicyControlEvidence.Valid,
                audioPayloadPolicyControlEvidence.IdentityMatchRows,
                Math.Max(1, audioPayloadPolicyControlEvidence.JoinedRows),
                "independent keyed reparse compares bounded 512-candidate and all-candidate EVT/ABKC payload reports, including decode status, PCM hashes and exact-match counters",
                "does not select an ABKC sample, bind an EVT name to gameplay or claim original playback semantics",
                $"bounded_rows={audioPayloadPolicyControlEvidence.BoundedRows}; all_rows={audioPayloadPolicyControlEvidence.AllRows}; joined_rows={audioPayloadPolicyControlEvidence.JoinedRows}; bounded_event_rows={audioPayloadPolicyControlEvidence.BoundedEventRows}; all_event_rows={audioPayloadPolicyControlEvidence.AllEventRows}; bounded_bank_rows={audioPayloadPolicyControlEvidence.BoundedBankRows}; all_bank_rows={audioPayloadPolicyControlEvidence.AllBankRows}; identity_match_rows={audioPayloadPolicyControlEvidence.IdentityMatchRows}; status_match_rows={audioPayloadPolicyControlEvidence.StatusMatchRows}; decode_match_rows={audioPayloadPolicyControlEvidence.DecodeMatchRows}; wave_bytes_match_rows={audioPayloadPolicyControlEvidence.WaveBytesMatchRows}; pcm_hash_match_rows={audioPayloadPolicyControlEvidence.PcmHashMatchRows}; exact_match_count_match_rows={audioPayloadPolicyControlEvidence.ExactMatchCountMatchRows}; exact_pcm_match_rows={audioPayloadPolicyControlEvidence.ExactPcmMatchRows}; status={audioPayloadPolicyControlEvidence.Status}; provenance={audioPayloadPolicyControlEvidence.Provenance}; original_semantics={audioPayloadPolicyControlEvidence.OriginalSemantics}"),
            new(
                "audio-bank-decoder-selection-control",
                audioBankDecoderSelectionEvidence.Valid ? "verified-structural-negative" : "not-present",
                audioBankDecoderSelectionEvidence.Valid,
                audioBankDecoderSelectionEvidence.ValidControlRows,
                Math.Max(1, audioBankDecoderSelectionEvidence.BatchRows),
                "independent join reconciles bounded batch-decoder selection with exhaustive ABKC/AST/BNK candidate rows and wave-byte output",
                "does not promote managed first-accepted candidate selection to original EVT selector, sample identity, gameplay event or playback semantics",
                $"batch_rows={audioBankDecoderSelectionEvidence.BatchRows}; exhaustive_bank_rows={audioBankDecoderSelectionEvidence.ExhaustiveBankRows}; unique_batch_banks={audioBankDecoderSelectionEvidence.UniqueBatchBanks}; unique_exhaustive_banks={audioBankDecoderSelectionEvidence.UniqueExhaustiveBanks}; batch_decoded_rows={audioBankDecoderSelectionEvidence.BatchDecodedRows}; batch_rejected_rows={audioBankDecoderSelectionEvidence.BatchRejectedRows}; selected_identity_rows={audioBankDecoderSelectionEvidence.SelectedIdentityRows}; selected_kind_match_rows={audioBankDecoderSelectionEvidence.SelectedKindMatchRows}; selected_wave_bytes_match_rows={audioBankDecoderSelectionEvidence.SelectedWaveBytesMatchRows}; rejected_policy_match_rows={audioBankDecoderSelectionEvidence.RejectedPolicyMatchRows}; valid_control_rows={audioBankDecoderSelectionEvidence.ValidControlRows}; status={audioBankDecoderSelectionEvidence.Status}; provenance={audioBankDecoderSelectionEvidence.Provenance}; direct_selection_binding={audioBankDecoderSelectionEvidence.DirectSelectionBinding}; original_semantics={audioBankDecoderSelectionEvidence.OriginalSemantics}"),
            new(
                "audio-event-asset-path-index-control",
                audioEventAssetPathControlVerified ? "verified-structural-negative" : "not-present",
                audioEventAssetPathControlVerified,
                ReadInt(audioEventAssetPathSummary, "matched_path_rows"),
                Math.Max(1, ReadInt(audioEventAssetPathSummary, "xbe_literal_rows")),
                "independent join of four retail-XBE EVENTSPA.EVT literals with the exact ISO path and the 17-name EVT index report",
                "does not recover an executable loader, select an ABKC/AST/BNK sample, or claim original playback semantics",
                $"event_path={Get(audioEventAssetPathSummary, "event_path")}; candidate_name_count={Get(audioEventAssetPathSummary, "candidate_name_count")}; candidate_pairs={Get(audioEventAssetPathSummary, "candidate_pairs")}; xbe_literal_rows={Get(audioEventAssetPathSummary, "xbe_literal_rows")}; xbe_literal_occurrences={Get(audioEventAssetPathSummary, "xbe_literal_occurrences")}; xbe_literal_xref_count={Get(audioEventAssetPathSummary, "xbe_literal_xref_count")}; matched_path_rows={Get(audioEventAssetPathSummary, "matched_path_rows")}; distinct_matched_paths={Get(audioEventAssetPathSummary, "distinct_matched_paths")}; status={Get(audioEventAssetPathSummary, "status")}; provenance={Get(audioEventAssetPathSummary, "provenance")}; direct_selection_binding={Get(audioEventAssetPathSummary, "direct_selection_binding")}; original_semantics={Get(audioEventAssetPathSummary, "original_semantics")}"),
            new(
                "audio-event-static-context-global-assignment-control",
                audioEventStaticContextEvidence.Valid ? "verified-structural-negative" : "not-present",
                audioEventStaticContextEvidence.Valid,
                audioEventStaticContextEvidence.ValidEventRows,
                Math.Max(1, audioEventStaticContextEvidence.EventRows),
                "independent global reconciliation of retail-XBE audio-literal context, EVENTSPA.EVT path literals and the decoded EVT/ABKC payload domain",
                "does not infer an executable selector, EVT→ABKC/AST/BNK sample identity, gameplay event or original playback semantics",
                $"static_audio_xref_rows={audioEventStaticContextEvidence.StaticAudioXrefRows}; unique_audio_literals={audioEventStaticContextEvidence.UniqueAudioLiterals}; audio_context_rows={audioEventStaticContextEvidence.AudioContextRows}; audio_context_with_executable_calls={audioEventStaticContextEvidence.AudioContextWithExecutableCalls}; distinct_executable_call_targets={audioEventStaticContextEvidence.DistinctExecutableCallTargets}; audio_targets_with_multiple_literals={audioEventStaticContextEvidence.AudioTargetsWithMultipleLiterals}; nested_audio_literal_matches={audioEventStaticContextEvidence.NestedAudioLiteralMatches}; xdvdfs_audio_literal_matches={audioEventStaticContextEvidence.XdvdfsAudioLiteralMatches}; audio_event_literal_xref_rows={audioEventStaticContextEvidence.AudioEventLiteralXrefRows}; event_path_candidate_name_count={audioEventStaticContextEvidence.EventPathCandidateNameCount}; event_path_candidate_pairs={audioEventStaticContextEvidence.EventPathCandidatePairs}; event_path_literal_rows={audioEventStaticContextEvidence.EventPathLiteralRows}; event_path_literal_occurrences={audioEventStaticContextEvidence.EventPathLiteralOccurrences}; event_path_literal_xref_count={audioEventStaticContextEvidence.EventPathLiteralXrefCount}; event_path_matched_rows={audioEventStaticContextEvidence.EventPathMatchedRows}; event_path_distinct_matched_paths={audioEventStaticContextEvidence.EventPathDistinctMatchedPaths}; event_rows={audioEventStaticContextEvidence.EventRows}; bank_candidate_pool_size={audioEventStaticContextEvidence.BankCandidatePoolSize}; event_exact_pcm_match_rows={audioEventStaticContextEvidence.EventExactPcmMatchRows}; event_hashes_with_match={audioEventStaticContextEvidence.EventHashesWithMatch}; unique_evt_abkc_sample_binding_proven_rows={audioEventStaticContextEvidence.UniqueEvtAbkcSampleBindingProvenRows}; event_rows_without_static_event_xref={audioEventStaticContextEvidence.EventRowsWithoutStaticEventXref}; static_event_context_target_pool_size={audioEventStaticContextEvidence.StaticEventContextTargetPoolSize}; event_static_candidate_model_sum_before={audioEventStaticContextEvidence.EventStaticCandidateModelSumBefore}; event_static_candidate_model_sum_after={audioEventStaticContextEvidence.EventStaticCandidateModelSumAfter}; event_static_context_reduced_rows={audioEventStaticContextEvidence.EventStaticContextReducedRows}; unique_event_static_selector_proven_rows={audioEventStaticContextEvidence.UniqueEventStaticSelectorProvenRows}; valid_event_rows={audioEventStaticContextEvidence.ValidEventRows}; status={audioEventStaticContextEvidence.Status}; semantic_binding={audioEventStaticContextEvidence.SemanticBinding}; direct_selection_binding={audioEventStaticContextEvidence.DirectSelectionBinding}; provenance={audioEventStaticContextEvidence.Provenance}; original_semantics={audioEventStaticContextEvidence.OriginalSemantics}"),
            new(
                "animation-authored-runtime",
                authoredAnimationRuntimeVerified ? "verified-authored" : "not-present",
                authoredAnimationRuntimeVerified,
                authoredAnimationSamples,
                2,
                "authored procedural poses serialized from the managed renderer path",
                "independent timing/pose/rig evidence and a validated original decoder before changing the structural-only animation gate",
                $"animation_samples={authoredAnimationSamples}; animation_trace_written={Get(integrationReport, "animation_trace_written")}; original_semantics={Get(integrationReport, "original_semantics")}"),
            new(
                "animation-authored-assets",
                authoredAnimationAssetRuntimeVerified ? "verified-authored" : "not-present",
                authoredAnimationAssetRuntimeVerified,
                authoredAnimationAssetRuntimeVerified ? 1 : 0,
                1,
                "authored animation-profile manifest resolved by the managed clean-room animator",
                "keep authored profiles separate from original AXT records, frames, timing, rigging, skinning and morph semantics",
                $"authored_animation_asset_manifest={authoredAnimationAssetManifest}; manifest_complete={CleanRoomAnimationAssetManifest.Default.IsComplete}; original_semantics={Get(integrationReport, "original_semantics")}"),
            new(
                "animation-catalog-payload-names",
                animationPayloadNameVerified ? "verified-structural" : "not-present",
                animationPayloadNameVerified,
                animationPayloadNameTaggedRows,
                Math.Max(1, catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count)),
                "validated AXT catalog names repeated as tagged printable strings in the corresponding .data payload",
                "independent record-boundary, frame, pose, rig and timing evidence before interpreting the repeated names",
                $"banks={animationPayloadNameSummaryRows.Count}; catalog_rows={animationPayloadNameCatalogRows}; tagged_rows={animationPayloadNameTaggedRows}; missing_rows={animationPayloadNameMissingRows}; ambiguous_rows={animationPayloadNameAmbiguousRows}; payload_string_count={animationPayloadNameStringCount}; original_semantics=not-claimed"),
            new(
                "animation-catalog-payload-anchors",
                animationPayloadAnchorVerified ? "verified-structural" : "not-present",
                animationPayloadAnchorVerified,
                animationPayloadAnchorCompleteRows,
                Math.Max(1, catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count)),
                "ordered offsets of the two observed printable payload-name anchors per tagged catalog row",
                "independent record boundaries, frame, pose, rig, timing and decoder evidence before interpreting intervals",
                $"banks={animationPayloadAnchorBanks}; catalog_rows={animationPayloadAnchorCatalogRows}; tagged_rows={animationPayloadAnchorTaggedRows}; rows_with_two_anchors={animationPayloadAnchorCompleteRows}; anchor_count={animationPayloadAnchorCount}; banks_strictly_increasing={animationPayloadAnchorIncreasingBanks}; banks_with_complete_pairs={animationPayloadAnchorCompleteBanks}; original_semantics=not-claimed"),
            new(
                "animation-catalog-tail-range",
                animationTailRangeVerified ? "verified-structural" : "not-present",
                animationTailRangeVerified,
                animationTailRangeVerified ? ReadInt(animationTailRangeSummary, "verified_structural_rows") : 0,
                Math.Max(1, catalog.AnimationBanks.Count),
                "fixed __AnimationBank tail arithmetic repeats a 2x catalog count and an aligned 8x catalog-row byte span across every validated bank",
                "independent payload record, frame, pose, rig, timing and decoder evidence before interpreting the anonymous span or pairing it with catalog names",
                $"catalog_banks={Get(animationTailRangeSummary, "catalog_banks")}; validated_banks={Get(animationTailRangeSummary, "validated_banks")}; catalog_rows={Get(animationTailRangeSummary, "catalog_rows")}; tail_header_shape_valid_rows={Get(animationTailRangeSummary, "tail_header_shape_valid_rows")}; tail_entry_count_match_rows={Get(animationTailRangeSummary, "tail_entry_count_match_rows")}; tail_range_within_data_rows={Get(animationTailRangeSummary, "tail_range_within_data_rows")}; tail_range_aligned_rows={Get(animationTailRangeSummary, "tail_range_aligned_rows")}; tail_range_length_match_rows={Get(animationTailRangeSummary, "tail_range_length_match_rows")}; tail_range_relocation_count_match_rows={Get(animationTailRangeSummary, "tail_range_relocation_count_match_rows")}; rows_with_no_relocations_in_tail_range={Get(animationTailRangeSummary, "rows_with_no_relocations_in_tail_range")}; verified_structural_rows={Get(animationTailRangeSummary, "verified_structural_rows")}; status={Get(animationTailRangeSummary, "status")}; candidate_record_binding={Get(animationTailRangeSummary, "candidate_record_binding")}; original_semantics={Get(animationTailRangeSummary, "original_semantics")}"),
            new(
                "animation-catalog-tail-pair-relocations",
                animationTailPairVerified ? "verified-structural" : "not-present",
                animationTailPairVerified,
                animationTailPairVerified ? ReadInt(animationTailPairSummary, "banks_with_complete_pair_layout") : 0,
                Math.Max(1, catalog.AnimationBanks.Count),
                "anonymous __AnimationBank tail ordinals have two relocations at each 8-byte pair and distinct raw pointer/addend pairs within .data",
                "independent record/frame/pose/rig/timing decoder evidence and a proven ordinal-to-catalog-name join before interpreting the raw pair targets",
                $"catalog_banks={Get(animationTailPairSummary, "catalog_banks")}; validated_banks={Get(animationTailPairSummary, "validated_banks")}; catalog_rows={Get(animationTailPairSummary, "catalog_rows")}; pair_ordinal_rows={Get(animationTailPairSummary, "pair_ordinal_rows")}; pair_rows_with_two_relocations={Get(animationTailPairSummary, "pair_rows_with_two_relocations")}; pair_rows_with_expected_offsets={Get(animationTailPairSummary, "pair_rows_with_expected_offsets")}; rows_with_unexpected_relocation_multiplicity={Get(animationTailPairSummary, "rows_with_unexpected_relocation_multiplicity")}; first_raw_words_within_data={Get(animationTailPairSummary, "first_raw_words_within_data")}; second_raw_words_within_data={Get(animationTailPairSummary, "second_raw_words_within_data")}; first_raw_words_at_known_symbols={Get(animationTailPairSummary, "first_raw_words_at_known_symbols")}; second_raw_words_at_known_symbols={Get(animationTailPairSummary, "second_raw_words_at_known_symbols")}; distinct_raw_word_pairs={Get(animationTailPairSummary, "distinct_raw_word_pairs")}; banks_with_complete_pair_layout={Get(animationTailPairSummary, "banks_with_complete_pair_layout")}; status={Get(animationTailPairSummary, "status")}; candidate_record_binding={Get(animationTailPairSummary, "candidate_record_binding")}; original_semantics={Get(animationTailPairSummary, "original_semantics")}"),
            new(
                "animation-catalog-tail-target-chain",
                animationTailTargetChainVerified ? "verified-structural" : "not-present",
                animationTailTargetChainVerified,
                animationTailTargetChainVerified ? ReadInt(animationTailTargetChainSummary, "banks_with_complete_target_chain") : 0,
                Math.Max(1, catalog.AnimationBanks.Count),
                "bounded nonempty relocation window from each opaque __AnimationBank pair target, retaining multiplicity and raw-word bounds without one-hop semantic resolution",
                "independent record/frame/pose/rig/timing decoder evidence and a proven ordinal-to-catalog-name join before interpreting the chain",
                $"catalog_banks={Get(animationTailTargetChainSummary, "catalog_banks")}; validated_banks={Get(animationTailTargetChainSummary, "validated_banks")}; catalog_rows={Get(animationTailTargetChainSummary, "catalog_rows")}; pair_rows={Get(animationTailTargetChainSummary, "pair_rows")}; first_target_rows_with_one_relocation={Get(animationTailTargetChainSummary, "first_target_rows_with_one_relocation")}; second_target_rows_with_one_relocation={Get(animationTailTargetChainSummary, "second_target_rows_with_one_relocation")}; first_second_level_words_within_data={Get(animationTailTargetChainSummary, "first_second_level_words_within_data")}; second_second_level_words_within_data={Get(animationTailTargetChainSummary, "second_second_level_words_within_data")}; first_second_level_words_at_known_symbols={Get(animationTailTargetChainSummary, "first_second_level_words_at_known_symbols")}; second_second_level_words_at_known_symbols={Get(animationTailTargetChainSummary, "second_second_level_words_at_known_symbols")}; rows_with_complete_target_chain={Get(animationTailTargetChainSummary, "rows_with_complete_target_chain")}; banks_with_complete_target_chain={Get(animationTailTargetChainSummary, "banks_with_complete_target_chain")}; status={Get(animationTailTargetChainSummary, "status")}; candidate_record_binding={Get(animationTailTargetChainSummary, "candidate_record_binding")}; original_semantics={Get(animationTailTargetChainSummary, "original_semantics")}; one_hop_resolution=not-proven"),
            new(
                "animation-catalog-timestamp-metadata",
                animationTimestampAuditVerified ? "verified-structural-negative" : "not-present",
                animationTimestampAuditVerified,
                animationTimestampAuditVerified ? ReadInt(animationTimestampAuditSummary, "exact_timestamp_date_rows") : 0,
                Math.Max(1, catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count)),
                "numeric AXT catalog timestamps exactly reproduce the calendar date printed on their own source lines",
                "independent frame-duration, pose, rig, timing and decoder evidence before assigning animation semantics",
                $"banks={Get(animationTimestampAuditSummary, "bank_count")}; catalog_rows={Get(animationTimestampAuditSummary, "catalog_rows")}; display_date_parseable_rows={Get(animationTimestampAuditSummary, "display_date_parseable_rows")}; exact_timestamp_date_rows={Get(animationTimestampAuditSummary, "exact_timestamp_date_rows")}; timestamp_date_mismatch_rows={Get(animationTimestampAuditSummary, "timestamp_date_mismatch_rows")}; invalid_timestamp_rows={Get(animationTimestampAuditSummary, "invalid_timestamp_rows")}; inferred_utc_offset_minutes={Get(animationTimestampAuditSummary, "inferred_utc_offset_minutes")}; direct_timing_binding={Get(animationTimestampAuditSummary, "direct_timing_binding")}; original_semantics={Get(animationTimestampAuditSummary, "original_semantics")}"),
            new(
                "animation-morph-pose-bank-ambiguity-control",
                animationMorphPoseBankAmbiguityControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                animationMorphPoseBankAmbiguityControlEvidence.Valid,
                animationMorphPoseBankAmbiguityControlEvidence.ValidPoseRows,
                Math.Max(1, animationMorphPoseBankAmbiguityControlEvidence.PoseRows),
                "independent TSV reparse reconciles pose-bank population, animation-bank ELF symbol metadata and morph-layout containment while retaining the unresolved binding result",
                "does not assign pose symbols to animation banks or morph layouts to frames, timing, rigs, skinning, deformation or playback semantics",
                $"pose_rows={animationMorphPoseBankAmbiguityControlEvidence.PoseRows}; distinct_pose_archives={animationMorphPoseBankAmbiguityControlEvidence.DistinctPoseArchives}; distinct_pose_symbols={animationMorphPoseBankAmbiguityControlEvidence.DistinctPoseSymbols}; distinct_normalized_names={animationMorphPoseBankAmbiguityControlEvidence.DistinctNormalizedNames}; animation_bank_rows={animationMorphPoseBankAmbiguityControlEvidence.AnimationBankRows}; distinct_animation_banks={animationMorphPoseBankAmbiguityControlEvidence.DistinctAnimationBanks}; animation_bank_valid_rows={animationMorphPoseBankAmbiguityControlEvidence.AnimationBankValidRows}; animation_bank_symbol_metadata_rows={animationMorphPoseBankAmbiguityControlEvidence.AnimationBankSymbolMetadataRows}; morph_layout_rows={animationMorphPoseBankAmbiguityControlEvidence.MorphLayoutRows}; morph_parse_valid_rows={animationMorphPoseBankAmbiguityControlEvidence.MorphParseValidRows}; morph_containing_pairs={animationMorphPoseBankAmbiguityControlEvidence.MorphContainingPairs}; morph_exact_gap_set_layouts={animationMorphPoseBankAmbiguityControlEvidence.MorphExactGapSetLayouts}; morph_exact_gap_set_pairs={animationMorphPoseBankAmbiguityControlEvidence.MorphExactGapSetPairs}; morph_exact_gap_histogram_layouts={animationMorphPoseBankAmbiguityControlEvidence.MorphExactGapHistogramLayouts}; morph_exact_gap_histogram_pairs={animationMorphPoseBankAmbiguityControlEvidence.MorphExactGapHistogramPairs}; pose_name_match_rows={animationMorphPoseBankAmbiguityControlEvidence.PoseNameMatchRows}; pose_symbol_metadata_match_rows={animationMorphPoseBankAmbiguityControlEvidence.PoseSymbolMetadataMatchRows}; unique_pose_animation_binding_proven_rows={animationMorphPoseBankAmbiguityControlEvidence.UniquePoseAnimationBindingProvenRows}; valid_pose_rows={animationMorphPoseBankAmbiguityControlEvidence.ValidPoseRows}; status={animationMorphPoseBankAmbiguityControlEvidence.Status}; semantic_binding={animationMorphPoseBankAmbiguityControlEvidence.SemanticBinding}; provenance={animationMorphPoseBankAmbiguityControlEvidence.Provenance}; original_semantics={animationMorphPoseBankAmbiguityControlEvidence.OriginalSemantics}"),
            new(
                "animation-catalog-pose-timing-global-assignment-control",
                animationCatalogPoseTimingGlobalAssignmentControlEvidence.Valid ? "verified-structural-negative" : "not-present",
                animationCatalogPoseTimingGlobalAssignmentControlEvidence.Valid,
                animationCatalogPoseTimingGlobalAssignmentControlEvidence.ValidPoseRows,
                Math.Max(1, animationCatalogPoseTimingGlobalAssignmentControlEvidence.PoseRows),
                "independent global reconciliation of catalog timestamps, payload anchors, pose-bank rows and morph containment",
                "does not assign catalog metadata to frame duration, pose, rig, skinning, deformation or playback without a direct join key",
                $"catalog_bank_count={animationCatalogPoseTimingGlobalAssignmentControlEvidence.CatalogBankCount}; catalog_rows={animationCatalogPoseTimingGlobalAssignmentControlEvidence.CatalogRows}; catalog_timestamp_valid_rows={animationCatalogPoseTimingGlobalAssignmentControlEvidence.CatalogTimestampValidRows}; anchor_bank_rows={animationCatalogPoseTimingGlobalAssignmentControlEvidence.AnchorBankRows}; anchor_complete_bank_rows={animationCatalogPoseTimingGlobalAssignmentControlEvidence.AnchorCompleteBankRows}; anchor_count={animationCatalogPoseTimingGlobalAssignmentControlEvidence.AnchorCount}; pose_rows={animationCatalogPoseTimingGlobalAssignmentControlEvidence.PoseRows}; distinct_pose_archives={animationCatalogPoseTimingGlobalAssignmentControlEvidence.DistinctPoseArchives}; distinct_pose_symbols={animationCatalogPoseTimingGlobalAssignmentControlEvidence.DistinctPoseSymbols}; distinct_normalized_names={animationCatalogPoseTimingGlobalAssignmentControlEvidence.DistinctNormalizedNames}; morph_layout_rows={animationCatalogPoseTimingGlobalAssignmentControlEvidence.MorphLayoutRows}; morph_parse_valid_rows={animationCatalogPoseTimingGlobalAssignmentControlEvidence.MorphParseValidRows}; morph_containing_layouts={animationCatalogPoseTimingGlobalAssignmentControlEvidence.MorphContainingLayouts}; morph_containing_pairs={animationCatalogPoseTimingGlobalAssignmentControlEvidence.MorphContainingPairs}; morph_unique_containment_layouts={animationCatalogPoseTimingGlobalAssignmentControlEvidence.MorphUniqueContainmentLayouts}; morph_ambiguous_containment_layouts={animationCatalogPoseTimingGlobalAssignmentControlEvidence.MorphAmbiguousContainmentLayouts}; morph_exact_gap_set_pairs={animationCatalogPoseTimingGlobalAssignmentControlEvidence.MorphExactGapSetPairs}; pose_rows_without_catalog_join_key={animationCatalogPoseTimingGlobalAssignmentControlEvidence.PoseRowsWithoutCatalogJoinKey}; pose_rows_without_morph_join_key={animationCatalogPoseTimingGlobalAssignmentControlEvidence.PoseRowsWithoutMorphJoinKey}; pose_bank_domain_size_before={animationCatalogPoseTimingGlobalAssignmentControlEvidence.PoseBankDomainSizeBefore}; pose_bank_domain_size_after={animationCatalogPoseTimingGlobalAssignmentControlEvidence.PoseBankDomainSizeAfter}; global_assignment_models_before={animationCatalogPoseTimingGlobalAssignmentControlEvidence.GlobalAssignmentModelsBefore}; global_assignment_models_after={animationCatalogPoseTimingGlobalAssignmentControlEvidence.GlobalAssignmentModelsAfter}; pose_bank_domain_reduced_rows={animationCatalogPoseTimingGlobalAssignmentControlEvidence.PoseBankDomainReducedRows}; unique_pose_animation_binding_proven_rows={animationCatalogPoseTimingGlobalAssignmentControlEvidence.UniquePoseAnimationBindingProvenRows}; valid_pose_rows={animationCatalogPoseTimingGlobalAssignmentControlEvidence.ValidPoseRows}; status={animationCatalogPoseTimingGlobalAssignmentControlEvidence.Status}; semantic_binding={animationCatalogPoseTimingGlobalAssignmentControlEvidence.SemanticBinding}; provenance={animationCatalogPoseTimingGlobalAssignmentControlEvidence.Provenance}; original_semantics={animationCatalogPoseTimingGlobalAssignmentControlEvidence.OriginalSemantics}"),
            new(
                "audio",
                namedAudio > 0 && abkValidated > 0 ? "partially-integrated" : "incomplete",
                false,
                namedAudio + abkValidated,
                namedAudio + abkAttempted,
                "named EVT payload decode plus ABK/Xbox IMA validation and authored clean-room cue policy",
                "independent EVT/ABKC/sample selection evidence and bindings beyond the authored candidate routes",
                $"named_candidates={namedAudio}; payload_layout_candidates={namedAudioWithPayloadLayout}; abk_validated={abkValidated}; abk_rejected={abkRejected}; authored_route_count={authoredAudioRouteCount}; authored_unique_cues={authoredAudioUniqueCueCount}; authored_routes_available={authoredAudioRoutesAvailable}; authored_routes_missing={authoredAudioRouteCount - authoredAudioRoutesAvailable}; authored_routes={CleanRoomAudioCuePolicy.FormatRoutes()}; authored_audio_trace_replay={(authoredAudioTraceReplayVerified ? "verified" : "not-present")}; authored_audio_trace_events={authoredAudioTraceRows.Count}; authored_audio_trace_has_kickoff={authoredAudioTraceHasKickoff}; authored_audio_trace_all_played={authoredAudioTraceAllPlayed}; authored_steal_audio_trace_replay={(authoredStealAudioTraceReplayVerified ? "verified" : "not-present")}; authored_steal_audio_trace_events={authoredStealAudioTraceRows.Count}; authored_steal_audio_trace_has_tackle={authoredStealAudioTraceHasTackle}; gameplay_audio_pcm_bytes={Get(integrationReport, "gameplay_audio_pcm_bytes")}; gameplay_audio_pcm_sha256={Get(integrationReport, "gameplay_audio_pcm_sha256")}; audio_event_index_report={(audioEventIndexReportVerified ? "verified-structural-negative" : "not-present")}; audio_event_index_candidate_pairs={Get(audioEventIndexReport, "candidate_pairs")}; audio_event_index_hash_candidates={Get(audioEventIndexReport, "string_hash_family_candidates")}; audio_event_index_abk_banks={Get(audioEventIndexReport, "abk_banks_scanned")}; audio_event_index_abk_occurrences={Get(audioEventIndexReport, "abk_aligned_occurrences")}; audio_event_index_abk_values_with_hits={Get(audioEventIndexReport, "abk_values_with_hits")}; audio_event_index_header_occurrences={Get(audioEventIndexReport, "pcpahdr_big_aligned_occurrences")}; audio_event_index_header_values_with_hits={Get(audioEventIndexReport, "pcpahdr_big_values_with_hits")}; audio_event_index_data_occurrences={Get(audioEventIndexReport, "pcpadat_big_aligned_occurrences")}; audio_event_index_data_values_with_hits={Get(audioEventIndexReport, "pcpadat_big_values_with_hits")}; audio_event_index_direct_binding={Get(audioEventIndexReport, "direct_selection_binding")}; fifast2_csi_tuna_report={(fifast2CsiTunaVerified ? "verified-structural" : "not-present")}; fifast2_csi_tuna_rows={Get(fifast2CsiTunaSummary, "csi_rows_with_tuna_hash_match")}; fifast2_csi_tuna_hash_matches={Get(fifast2CsiTunaSummary, "tuna_hash_match_count")}; fifast2_csi_tuna_name_overlap={Get(fifast2CsiTunaSummary, "csi_name_overlap_count")}; fifast2_csi_tuna_opaque_hash_matches={Get(fifast2CsiTunaSummary, "csi_opaque_hash_match_count")}; fifast2_csi_tuna_direct_binding={Get(fifast2CsiTunaSummary, "direct_selection_binding")}; audio_payload_exhaustive_report={(audioPayloadWaveExhaustiveVerified ? "verified-structural-negative" : "not-present")}; audio_payload_exhaustive_event_candidates={Get(audioPayloadWaveExhaustiveSummary, "event_candidates")}; audio_payload_exhaustive_event_decoded={Get(audioPayloadWaveExhaustiveSummary, "event_decoded")}; audio_payload_exhaustive_bank_count={Get(audioPayloadWaveExhaustiveSummary, "bank_count")}; audio_payload_exhaustive_bank_sound_candidates={Get(audioPayloadWaveExhaustiveSummary, "bank_sound_candidates")}; audio_payload_exhaustive_bank_sound_decoded={Get(audioPayloadWaveExhaustiveSummary, "bank_sound_decoded")}; audio_payload_exhaustive_exact_pcm_hash_pairs={Get(audioPayloadWaveExhaustiveSummary, "exact_pcm_hash_pairs")}; audio_payload_exhaustive_event_hashes_with_match={Get(audioPayloadWaveExhaustiveSummary, "event_hashes_with_match")}; audio_payload_exhaustive_bounded_policy={Get(audioPayloadWaveExhaustiveSummary, "bounded_policy")}; audio_payload_exhaustive_direct_binding={Get(audioPayloadWaveExhaustiveSummary, "direct_selection_binding")}; audio_payload_all_report={(audioPayloadWaveAllVerified ? "verified-structural-negative" : "not-present")}; audio_payload_all_event_candidates={Get(audioPayloadWaveAllSummary, "event_candidates")}; audio_payload_all_event_decoded={Get(audioPayloadWaveAllSummary, "event_decoded")}; audio_payload_all_bank_count={Get(audioPayloadWaveAllSummary, "bank_count")}; audio_payload_all_bank_sound_candidates={Get(audioPayloadWaveAllSummary, "bank_sound_candidates")}; audio_payload_all_bank_sound_decoded={Get(audioPayloadWaveAllSummary, "bank_sound_decoded")}; audio_payload_all_exact_pcm_hash_pairs={Get(audioPayloadWaveAllSummary, "exact_pcm_hash_pairs")}; audio_payload_all_event_hashes_with_match={Get(audioPayloadWaveAllSummary, "event_hashes_with_match")}; audio_payload_all_policy={Get(audioPayloadWaveAllSummary, "bounded_policy")}; audio_payload_all_direct_binding={Get(audioPayloadWaveAllSummary, "direct_selection_binding")}; authored_audio_coverage_report={(audioCoverageVerified ? "verified-authored" : "not-present")}; authored_audio_coverage_scenarios={Get(audioCoverageSummary, "scenario_count")}; authored_audio_coverage_verified_scenarios={Get(audioCoverageSummary, "verified_scenario_count")}; authored_audio_coverage_events={Get(audioCoverageSummary, "total_events")}; authored_audio_coverage_prepared={Get(audioCoverageSummary, "total_prepared")}; authored_audio_coverage_played={Get(audioCoverageSummary, "total_played")}; authored_audio_coverage_unmapped={Get(audioCoverageSummary, "total_unmapped")}; authored_audio_coverage_actions={Get(audioCoverageSummary, "covered_policy_actions")}; authored_audio_coverage_missing_actions={Get(audioCoverageSummary, "missing_policy_actions")}"),
            new(
                "audio-aimed-trace",
                authoredAimedAudioTraceReplayVerified ? "verified-authored" : "not-present",
                authoredAimedAudioTraceReplayVerified,
                authoredAimedAudioTraceRows.Count,
                6,
                "authored --aimed-demo audio trace/replay with explicit clean-room cue routes",
                "independent original event/sample selection evidence before treating these cues as decoded audio",
                $"authored_aimed_audio_trace_replay={(authoredAimedAudioTraceReplayVerified ? "verified" : "not-present")}; authored_aimed_audio_trace_events={authoredAimedAudioTraceRows.Count}; authored_aimed_audio_trace_has_shot={authoredAimedAudioTraceHasShot}; authored_aimed_audio_trace_has_goal={authoredAimedAudioTraceHasGoal}; authored_aimed_audio_trace_all_played={authoredAimedAudioTraceAllPlayed}; original_semantics=not-claimed"),
            new(
                "audio-event-payload-correlation",
                audioPayloadWaveCorrelationVerified ? "verified-structural" : "not-present",
                audioPayloadWaveCorrelationVerified,
                ReadInt(audioPayloadWaveCorrelationSummary, "event_decoded") + ReadInt(audioPayloadWaveCorrelationSummary, "bank_decoded"),
                Math.Max(1, catalog.AudioEventCandidates.Count + catalog.AudioBanks.Count),
                "bounded PCM hash comparison between decoded named EVT candidates and decoded ABKC/AST candidates",
                "independent event/sample selection evidence before interpreting any payload correlation as an original audio binding",
                $"event_candidates={Get(audioPayloadWaveCorrelationSummary, "event_candidates")}; event_decoded={Get(audioPayloadWaveCorrelationSummary, "event_decoded")}; bank_candidates={Get(audioPayloadWaveCorrelationSummary, "bank_candidates")}; bank_decoded={Get(audioPayloadWaveCorrelationSummary, "bank_decoded")}; exact_pcm_hash_pairs={Get(audioPayloadWaveCorrelationSummary, "exact_pcm_hash_pairs")}; event_hashes_with_match={Get(audioPayloadWaveCorrelationSummary, "event_hashes_with_match")}; bounded_policy={Get(audioPayloadWaveCorrelationSummary, "bounded_policy")}; direct_selection_binding={Get(audioPayloadWaveCorrelationSummary, "direct_selection_binding")}; original_semantics=not-claimed"),
            new(
                "audio-event-named-payload-identity-control",
                audioNamedPayloadIdentityEvidence.Valid ? "verified-structural-negative" : "not-present",
                audioNamedPayloadIdentityEvidence.Valid,
                audioNamedPayloadIdentityEvidence.PcmSha256MatchRows,
                Math.Max(1, audioNamedPayloadIdentityEvidence.JoinedEventRows),
                "independent reparse reconciles named EVT WAVE/PCM identity across runtime, named decoder and exhaustive payload reports",
                "does not select an ABKC sample, bind an EVT name to gameplay or claim original playback semantics",
                $"runtime_rows={audioNamedPayloadIdentityEvidence.RuntimeRows}; named_decode_rows={audioNamedPayloadIdentityEvidence.NamedDecodeRows}; correlation_event_rows={audioNamedPayloadIdentityEvidence.CorrelationEventRows}; correlation_bank_rows={audioNamedPayloadIdentityEvidence.CorrelationBankRows}; correlation_bank_decoded_rows={audioNamedPayloadIdentityEvidence.CorrelationBankDecodedRows}; joined_event_rows={audioNamedPayloadIdentityEvidence.JoinedEventRows}; missing_runtime_events={audioNamedPayloadIdentityEvidence.MissingRuntimeEvents}; missing_named_decode_events={audioNamedPayloadIdentityEvidence.MissingNamedDecodeEvents}; missing_correlation_events={audioNamedPayloadIdentityEvidence.MissingCorrelationEvents}; runtime_metadata_validated_rows={audioNamedPayloadIdentityEvidence.RuntimeMetadataValidatedRows}; runtime_prepared_rows={audioNamedPayloadIdentityEvidence.RuntimePreparedRows}; runtime_wave_header_valid_rows={audioNamedPayloadIdentityEvidence.RuntimeWaveHeaderValidRows}; named_decode_validated_rows={audioNamedPayloadIdentityEvidence.NamedDecodeValidatedRows}; correlation_decode_validated_rows={audioNamedPayloadIdentityEvidence.CorrelationDecodeValidatedRows}; wave_bytes_match_rows={audioNamedPayloadIdentityEvidence.WaveBytesMatchRows}; pcm_bytes_match_rows={audioNamedPayloadIdentityEvidence.PcmBytesMatchRows}; pcm_sha256_match_rows={audioNamedPayloadIdentityEvidence.PcmSha256MatchRows}; event_hashes_with_bank_match={audioNamedPayloadIdentityEvidence.EventHashesWithBankMatch}; exact_pcm_hash_pairs={audioNamedPayloadIdentityEvidence.ExactPcmHashPairs}; unique_event_names={audioNamedPayloadIdentityEvidence.UniqueEventNames}; status={audioNamedPayloadIdentityEvidence.Status}; provenance={audioNamedPayloadIdentityEvidence.Provenance}; direct_selection_binding={audioNamedPayloadIdentityEvidence.DirectSelectionBinding}; original_semantics={audioNamedPayloadIdentityEvidence.OriginalSemantics}"),
            new(
                "animation-deformation",
                "structural-only",
                false,
                catalog.AnimationBanks.Count + catalog.MorphSiblings.Sum(morph => morph.MorphEvidenceCount),
                catalog.AnimationBanks.Count,
                "validated animation-bank/morph containers, ELF metadata and sidecar boundaries",
                "independent timing/pose/rig evidence and a validated decoder before applying animation or skinning",
                $"animation_banks={catalog.AnimationBanks.Count}; morph_evidence={catalog.MorphSiblings.Sum(morph => morph.MorphEvidenceCount)}; relocation_profile_summary={(animationMorphRelocationSummary is null ? "not-present" : "verified-structural-control")}; relocation_catalog_rows={animationMorphCatalogRows}; relocation_layouts={animationMorphLayouts}; relocation_parse_valid_layouts={animationMorphParseValidLayouts}; relocation_invalid_layouts={animationMorphInvalidLayouts}; relocation_animation_banks={animationMorphBanks}; relocation_containing_layouts={animationMorphContainingLayouts}; relocation_exact_layouts={animationMorphExactLayouts}; relocation_containing_pairs={animationMorphContainingPairs}; relocation_exact_pairs={animationMorphExactPairs}; relocation_exact_histogram_layouts={animationMorphExactHistogramLayouts}; relocation_exact_histogram_pairs={animationMorphExactHistogramPairs}; no pose/rig/skinning binding"),
        };

        return new ProjectReadinessSummary(
            rows,
            rows.All(row => row.GatePassed),
            sha256,
            length,
            attributes);
    }

    public static void WriteReport(string path, ProjectReadinessSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "area",
                "status",
                "gate_passed",
                "observed",
                "expected",
                "evidence",
                "next_required_evidence",
                "notes",
            }),
        };
        lines.AddRange(summary.Rows.Select(row => string.Join("\t", new[]
        {
            Field(row.Area),
            Field(row.Status),
            row.GatePassed ? "true" : "false",
            row.Observed.ToString(CultureInfo.InvariantCulture),
            row.Expected.ToString(CultureInfo.InvariantCulture),
            Field(row.Evidence),
            Field(row.NextRequiredEvidence),
            Field(row.Notes),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string ComputeSha256(string path)
    {
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(SHA256.HashData(input));
    }

    private static WindowedFrameCaptureEvidence ReadWindowedFrameCapture(string path)
    {
        const string expectedHeader = "P6\n960 540\n255\n";
        var headerLength = Encoding.ASCII.GetByteCount(expectedHeader);
        if (!File.Exists(path))
        {
            return new(false, 0, string.Empty, false, string.Empty);
        }

        try
        {
            var bytes = File.ReadAllBytes(path);
            var header = bytes.Length >= headerLength
                ? Encoding.ASCII.GetString(bytes, 0, headerLength)
                : string.Empty;
            var nonUniform = false;
            if (bytes.Length > headerLength)
            {
                var firstPixelByte = bytes[headerLength];
                for (var index = headerLength + 1;
                     index < Math.Min(bytes.Length, headerLength + 200_000);
                     index++)
                {
                    if (bytes[index] != firstPixelByte)
                    {
                        nonUniform = true;
                        break;
                    }
                }
            }

            var expectedLength = checked(headerLength + (960 * 540 * 3));
            var valid = bytes.Length == expectedLength &&
                        string.Equals(header, expectedHeader, StringComparison.Ordinal) &&
                        nonUniform;
            return new(
                valid,
                bytes.LongLength,
                header,
                nonUniform,
                ComputeSha256(path));
        }
        catch (IOException)
        {
            return new(false, 0, string.Empty, false, string.Empty);
        }
        catch (UnauthorizedAccessException)
        {
            return new(false, 0, string.Empty, false, string.Empty);
        }
    }

    private static StaticRenderEvidence ReadStaticRenderEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\trender_xref_rows\tunique_render_literals\trender_context_rows\trender_context_with_executable_calls\tdistinct_executable_call_targets\trender_targets_with_multiple_literals\tcategory_counts\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var renderXrefRows = ReadInt(row, "render_xref_rows");
        var uniqueRenderLiterals = ReadInt(row, "unique_render_literals");
        var renderContextRows = ReadInt(row, "render_context_rows");
        var renderContextWithExecutableCalls = ReadInt(row, "render_context_with_executable_calls");
        var distinctExecutableCallTargets = ReadInt(row, "distinct_executable_call_targets");
        var categoryCounts = Get(row, "category_counts");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-static-render-literal-context", StringComparison.Ordinal) &&
                    renderXrefRows > 0 &&
                    uniqueRenderLiterals > 0 &&
                    renderContextRows > 0 &&
                    renderContextWithExecutableCalls > 0 &&
                    distinctExecutableCallTargets > 0 &&
                    !string.IsNullOrWhiteSpace(categoryCounts) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            renderXrefRows,
            uniqueRenderLiterals,
            renderContextRows,
            renderContextWithExecutableCalls,
            distinctExecutableCallTargets,
            categoryCounts,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderTargetEvidence ReadStaticRenderTargetEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcandidate_targets\tdisassembled_targets\tret_terminated_targets\tprologue_like_targets\tdirect_call_count\tindirect_call_count\tconditional_branch_count\tabsolute_memory_operand_count\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var candidateTargets = ReadInt(row, "candidate_targets");
        var disassembledTargets = ReadInt(row, "disassembled_targets");
        var retTerminatedTargets = ReadInt(row, "ret_terminated_targets");
        var prologueLikeTargets = ReadInt(row, "prologue_like_targets");
        var directCallCount = ReadInt(row, "direct_call_count");
        var indirectCallCount = ReadInt(row, "indirect_call_count");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-static-render-target-windows", StringComparison.Ordinal) &&
                    candidateTargets > 0 &&
                    disassembledTargets == candidateTargets &&
                    retTerminatedTargets > 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            candidateTargets,
            disassembledTargets,
            retTerminatedTargets,
            prologueLikeTargets,
            directCallCount,
            indirectCallCount,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderCfgEvidence ReadStaticRenderCfgEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcandidate_targets\tanalyzed_targets\ttargets_with_reachable_cfg\ttargets_with_reachable_ret\ttotal_linear_instructions\ttotal_reachable_instructions\ttotal_basic_blocks\ttotal_conditional_branches\ttotal_unconditional_branches\ttotal_internal_branch_targets\ttotal_out_of_window_branch_targets\ttotal_direct_calls\ttotal_unique_direct_call_targets\ttotal_indirect_control_transfers\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var candidateTargets = ReadInt(row, "candidate_targets");
        var analyzedTargets = ReadInt(row, "analyzed_targets");
        var targetsWithReachableCfg = ReadInt(row, "targets_with_reachable_cfg");
        var targetsWithReachableRet = ReadInt(row, "targets_with_reachable_ret");
        var totalBasicBlocks = ReadInt(row, "total_basic_blocks");
        var totalDirectCalls = ReadInt(row, "total_direct_calls");
        var totalIndirectControlTransfers = ReadInt(row, "total_indirect_control_transfers");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-static-render-cfg-slices", StringComparison.Ordinal) &&
                    candidateTargets > 0 &&
                    analyzedTargets == candidateTargets &&
                    targetsWithReachableCfg == analyzedTargets &&
                    targetsWithReachableRet > 0 &&
                    totalBasicBlocks > 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            candidateTargets,
            analyzedTargets,
            targetsWithReachableCfg,
            targetsWithReachableRet,
            totalBasicBlocks,
            totalDirectCalls,
            totalIndirectControlTransfers,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderCallTargetEvidence ReadStaticRenderCallTargetEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tsource_render_target_windows\tsource_windows_with_direct_calls\tunique_direct_call_targets\tmapped_executable_targets\tdisassembled_targets\tret_terminated_targets\tprologue_like_targets\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var sourceRenderTargetWindows = ReadInt(row, "source_render_target_windows");
        var sourceWindowsWithDirectCalls = ReadInt(row, "source_windows_with_direct_calls");
        var uniqueDirectCallTargets = ReadInt(row, "unique_direct_call_targets");
        var mappedExecutableTargets = ReadInt(row, "mapped_executable_targets");
        var disassembledTargets = ReadInt(row, "disassembled_targets");
        var retTerminatedTargets = ReadInt(row, "ret_terminated_targets");
        var prologueLikeTargets = ReadInt(row, "prologue_like_targets");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-static-render-call-target-slices", StringComparison.Ordinal) &&
                    sourceRenderTargetWindows > 0 &&
                    sourceWindowsWithDirectCalls > 0 &&
                    uniqueDirectCallTargets > 0 &&
                    mappedExecutableTargets == disassembledTargets &&
                    disassembledTargets == uniqueDirectCallTargets &&
                    retTerminatedTargets > 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            sourceRenderTargetWindows,
            sourceWindowsWithDirectCalls,
            uniqueDirectCallTargets,
            mappedExecutableTargets,
            disassembledTargets,
            retTerminatedTargets,
            prologueLikeTargets,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderCallGraphEvidence ReadStaticRenderCallGraphEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tseed_targets\tseed_mapped_targets\tmax_depth\tmax_nodes\tdiscovered_nodes\tmapped_executable_nodes\tdisassembled_nodes\tret_terminated_nodes\tnodes_with_direct_calls\tunique_edges\ttotal_edge_occurrences\ttruncated_by_max_nodes\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var seedTargets = ReadInt(row, "seed_targets");
        var seedMappedTargets = ReadInt(row, "seed_mapped_targets");
        var maxDepth = ReadInt(row, "max_depth");
        var discoveredNodes = ReadInt(row, "discovered_nodes");
        var mappedExecutableNodes = ReadInt(row, "mapped_executable_nodes");
        var disassembledNodes = ReadInt(row, "disassembled_nodes");
        var retTerminatedNodes = ReadInt(row, "ret_terminated_nodes");
        var nodesWithDirectCalls = ReadInt(row, "nodes_with_direct_calls");
        var uniqueEdges = ReadInt(row, "unique_edges");
        var totalEdgeOccurrences = ReadInt(row, "total_edge_occurrences");
        var truncated = bool.TryParse(Get(row, "truncated_by_max_nodes"), out var parsedTruncated) && parsedTruncated;
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-static-bounded-render-call-graph", StringComparison.Ordinal) &&
                    seedTargets > 0 &&
                    seedMappedTargets == seedTargets &&
                    maxDepth >= 0 &&
                    discoveredNodes > seedTargets &&
                    mappedExecutableNodes == disassembledNodes &&
                    disassembledNodes == discoveredNodes &&
                    retTerminatedNodes > 0 &&
                    nodesWithDirectCalls > 0 &&
                    uniqueEdges > 0 &&
                    totalEdgeOccurrences >= uniqueEdges &&
                    !truncated &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            seedTargets,
            seedMappedTargets,
            maxDepth,
            discoveredNodes,
            mappedExecutableNodes,
            disassembledNodes,
            retTerminatedNodes,
            nodesWithDirectCalls,
            uniqueEdges,
            totalEdgeOccurrences,
            truncated,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderIndirectTransferEvidence ReadStaticRenderIndirectTransferEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcandidate_targets\tanalyzed_targets\ttargets_with_indirect_transfers\ttotal_indirect_transfers\tindirect_calls\tindirect_jumps\tregister_operands\tmemory_operands\tabsolute_memory_operands\tdistinct_operand_shapes\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var candidateTargets = ReadInt(row, "candidate_targets");
        var analyzedTargets = ReadInt(row, "analyzed_targets");
        var targetsWithIndirectTransfers = ReadInt(row, "targets_with_indirect_transfers");
        var totalIndirectTransfers = ReadInt(row, "total_indirect_transfers");
        var indirectCalls = ReadInt(row, "indirect_calls");
        var indirectJumps = ReadInt(row, "indirect_jumps");
        var registerOperands = ReadInt(row, "register_operands");
        var memoryOperands = ReadInt(row, "memory_operands");
        var absoluteMemoryOperands = ReadInt(row, "absolute_memory_operands");
        var distinctOperandShapes = ReadInt(row, "distinct_operand_shapes");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-static-render-indirect-transfer-inventory", StringComparison.Ordinal) &&
                    candidateTargets > 0 &&
                    analyzedTargets == candidateTargets &&
                    targetsWithIndirectTransfers > 0 &&
                    totalIndirectTransfers > 0 &&
                    indirectCalls + indirectJumps == totalIndirectTransfers &&
                    registerOperands + memoryOperands == totalIndirectTransfers &&
                    absoluteMemoryOperands >= 0 &&
                    absoluteMemoryOperands <= memoryOperands &&
                    distinctOperandShapes > 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            candidateTargets,
            analyzedTargets,
            targetsWithIndirectTransfers,
            totalIndirectTransfers,
            indirectCalls,
            indirectJumps,
            registerOperands,
            memoryOperands,
            absoluteMemoryOperands,
            distinctOperandShapes,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderIndirectReferenceEvidence ReadStaticRenderIndirectReferenceEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tindirect_rows\treference_rows\tabsolute_memory_rows\tindexed_table_base_rows\tbounded_reference_reads\treference_words_valid\treference_words_in_executable_sections\tdistinct_reference_addresses\tdistinct_raw_words\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var indirectRows = ReadInt(row, "indirect_rows");
        var referenceRows = ReadInt(row, "reference_rows");
        var absoluteMemoryRows = ReadInt(row, "absolute_memory_rows");
        var indexedTableBaseRows = ReadInt(row, "indexed_table_base_rows");
        var boundedReferenceReads = ReadInt(row, "bounded_reference_reads");
        var referenceWordsValid = ReadInt(row, "reference_words_valid");
        var referenceWordsInExecutableSections = ReadInt(row, "reference_words_in_executable_sections");
        var distinctReferenceAddresses = ReadInt(row, "distinct_reference_addresses");
        var distinctRawWords = ReadInt(row, "distinct_raw_words");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-static-render-indirect-reference-words", StringComparison.Ordinal) &&
                    indirectRows > 0 &&
                    referenceRows > 0 &&
                    absoluteMemoryRows >= 0 &&
                    indexedTableBaseRows >= 0 &&
                    absoluteMemoryRows + indexedTableBaseRows == referenceRows &&
                    boundedReferenceReads == referenceRows &&
                    referenceWordsValid == boundedReferenceReads &&
                    referenceWordsInExecutableSections == referenceWordsValid &&
                    distinctReferenceAddresses > 0 &&
                    distinctRawWords > 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            indirectRows,
            referenceRows,
            absoluteMemoryRows,
            indexedTableBaseRows,
            boundedReferenceReads,
            referenceWordsValid,
            referenceWordsInExecutableSections,
            distinctReferenceAddresses,
            distinctRawWords,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderIndirectReferenceSliceEvidence ReadStaticRenderIndirectReferenceSliceEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\treference_rows\tdistinct_raw_words\texecutable_raw_words\tdisassembled_candidate_words\tret_terminated_candidate_words\tprologue_like_candidate_words\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var referenceRows = ReadInt(row, "reference_rows");
        var distinctRawWords = ReadInt(row, "distinct_raw_words");
        var executableRawWords = ReadInt(row, "executable_raw_words");
        var disassembledCandidateWords = ReadInt(row, "disassembled_candidate_words");
        var retTerminatedCandidateWords = ReadInt(row, "ret_terminated_candidate_words");
        var prologueLikeCandidateWords = ReadInt(row, "prologue_like_candidate_words");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-static-render-indirect-reference-word-slices", StringComparison.Ordinal) &&
                    referenceRows > 0 &&
                    distinctRawWords > 0 &&
                    executableRawWords == distinctRawWords &&
                    disassembledCandidateWords == executableRawWords &&
                    retTerminatedCandidateWords >= 0 &&
                    retTerminatedCandidateWords <= disassembledCandidateWords &&
                    prologueLikeCandidateWords >= 0 &&
                    prologueLikeCandidateWords <= disassembledCandidateWords &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            referenceRows,
            distinctRawWords,
            executableRawWords,
            disassembledCandidateWords,
            retTerminatedCandidateWords,
            prologueLikeCandidateWords,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderIndirectWindowJoinEvidence ReadStaticRenderIndirectWindowJoinEvidence(string path)
    {
        const string expectedHeader =
            "target_windows\ttargets_with_indirect_transfers\ttotal_indirect_transfers\tindirect_calls\tindirect_jumps\ttargets_with_references\treference_rows\ttargets_with_transfer_reference_instruction_overlap\texact_transfer_reference_instruction_rows\tdistinct_raw_words\texecutable_raw_words\tdisassembled_candidate_words\tret_terminated_candidate_words\tprologue_like_candidate_words\tstatus\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var targetWindows = ReadInt(row, "target_windows");
        var targetsWithIndirectTransfers = ReadInt(row, "targets_with_indirect_transfers");
        var totalIndirectTransfers = ReadInt(row, "total_indirect_transfers");
        var indirectCalls = ReadInt(row, "indirect_calls");
        var indirectJumps = ReadInt(row, "indirect_jumps");
        var targetsWithReferences = ReadInt(row, "targets_with_references");
        var referenceRows = ReadInt(row, "reference_rows");
        var targetsWithTransferReferenceInstructionOverlap = ReadInt(row, "targets_with_transfer_reference_instruction_overlap");
        var exactTransferReferenceInstructionRows = ReadInt(row, "exact_transfer_reference_instruction_rows");
        var distinctRawWords = ReadInt(row, "distinct_raw_words");
        var executableRawWords = ReadInt(row, "executable_raw_words");
        var disassembledCandidateWords = ReadInt(row, "disassembled_candidate_words");
        var retTerminatedCandidateWords = ReadInt(row, "ret_terminated_candidate_words");
        var prologueLikeCandidateWords = ReadInt(row, "prologue_like_candidate_words");
        var status = Get(row, "status");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-static-render-indirect-window-join", StringComparison.Ordinal) &&
                    targetWindows > 0 &&
                    targetsWithIndirectTransfers > 0 &&
                    targetsWithIndirectTransfers <= targetWindows &&
                    totalIndirectTransfers > 0 &&
                    indirectCalls >= 0 &&
                    indirectJumps >= 0 &&
                    indirectCalls + indirectJumps == totalIndirectTransfers &&
                    targetsWithReferences > 0 &&
                    targetsWithReferences <= targetWindows &&
                    referenceRows > 0 &&
                    targetsWithTransferReferenceInstructionOverlap >= 0 &&
                    targetsWithTransferReferenceInstructionOverlap <= targetsWithReferences &&
                    exactTransferReferenceInstructionRows > 0 &&
                    exactTransferReferenceInstructionRows <= referenceRows &&
                    distinctRawWords > 0 &&
                    executableRawWords == distinctRawWords &&
                    disassembledCandidateWords == executableRawWords &&
                    retTerminatedCandidateWords >= 0 &&
                    retTerminatedCandidateWords <= disassembledCandidateWords &&
                    prologueLikeCandidateWords >= 0 &&
                    prologueLikeCandidateWords <= disassembledCandidateWords &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            targetWindows,
            targetsWithIndirectTransfers,
            totalIndirectTransfers,
            indirectCalls,
            indirectJumps,
            targetsWithReferences,
            referenceRows,
            targetsWithTransferReferenceInstructionOverlap,
            exactTransferReferenceInstructionRows,
            distinctRawWords,
            executableRawWords,
            disassembledCandidateWords,
            retTerminatedCandidateWords,
            prologueLikeCandidateWords,
            status,
            originalSemantics);
    }

    private static StaticRenderIndirectIndexedTableEvidence ReadStaticRenderIndirectIndexedTableEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tindexed_rows\tdistinct_table_bases\ttables_scan_capped\ttables_terminated_after_non_executable_words\treadable_table_words\texecutable_table_words\tnon_executable_table_words\tunreadable_table_words\tdistinct_executable_words\ttables_with_contiguous_executable_prefix\tmax_contiguous_executable_prefix\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var indexedRows = ReadInt(row, "indexed_rows");
        var distinctTableBases = ReadInt(row, "distinct_table_bases");
        var tablesScanCapped = ReadInt(row, "tables_scan_capped");
        var tablesTerminatedAfterNonExecutableWords = ReadInt(row, "tables_terminated_after_non_executable_words");
        var readableTableWords = ReadInt(row, "readable_table_words");
        var executableTableWords = ReadInt(row, "executable_table_words");
        var nonExecutableTableWords = ReadInt(row, "non_executable_table_words");
        var unreadableTableWords = ReadInt(row, "unreadable_table_words");
        var distinctExecutableWords = ReadInt(row, "distinct_executable_words");
        var tablesWithContiguousExecutablePrefix = ReadInt(row, "tables_with_contiguous_executable_prefix");
        var maxContiguousExecutablePrefix = ReadInt(row, "max_contiguous_executable_prefix");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-bounded-static-render-indirect-indexed-table", StringComparison.Ordinal) &&
                    indexedRows > 0 &&
                    distinctTableBases > 0 &&
                    tablesScanCapped >= 0 &&
                    tablesTerminatedAfterNonExecutableWords >= 0 &&
                    readableTableWords > 0 &&
                    executableTableWords > 0 &&
                    nonExecutableTableWords >= 0 &&
                    unreadableTableWords >= 0 &&
                    executableTableWords + nonExecutableTableWords + unreadableTableWords == readableTableWords + unreadableTableWords &&
                    distinctExecutableWords > 0 &&
                    tablesWithContiguousExecutablePrefix > 0 &&
                    maxContiguousExecutablePrefix > 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            indexedRows,
            distinctTableBases,
            tablesScanCapped,
            tablesTerminatedAfterNonExecutableWords,
            readableTableWords,
            executableTableWords,
            nonExecutableTableWords,
            unreadableTableWords,
            distinctExecutableWords,
            tablesWithContiguousExecutablePrefix,
            maxContiguousExecutablePrefix,
            status,
            provenance,
            originalSemantics);
    }

    internal static ProjectReadinessRow ReadRenderParameterControl(string reportDirectory)
    {
        var rows = ReadTsvRows(Path.Combine(reportDirectory, "eagl-render-parameter-static-control-summary.tsv"));
        var row = rows.FirstOrDefault();
        var expected = new Dictionary<string, int>
        {
            ["primitive_rows"] = 2364, ["command_rows"] = 36779, ["parameter_pairs"] = 32051,
            ["fetch_base_record_matches"] = 11818, ["fetch_stride_matches"] = 11818,
            ["fetch_runtime_selector_unresolved"] = 7092, ["draw_commands"] = 2364,
            ["draw_condition_nonzero"] = 2364, ["tar_state_parameters"] = 2364,
            ["geo_state_parameters"] = 1612, ["primitives_with_both_state_parameters"] = 1612,
            ["static_spans_validated"] = 20, ["static_sites_validated"] = 106,
            ["gpu_method_headers_verified"] = 3,
            ["negative_controls_passed"] = 6,
        };
        bool HashMatches(string name, string field)
        {
            var path = Path.Combine(reportDirectory, name);
            if (!File.Exists(path) || !IsSha256Hex(Get(row, field)))
                return false;
            using var stream = File.OpenRead(path);
            return string.Equals(Convert.ToHexString(SHA256.HashData(stream)), Get(row, field), StringComparison.Ordinal);
        }
        var hashesMatch = HashMatches("eagl-render-parameter-audit.tsv", "parameter_report_sha256") &&
                          HashMatches("eagl-original-render-chain.tsv", "chain_report_sha256") &&
                          HashMatches("eagl-render-parameter-static-control.tsv", "static_report_sha256");
        var valid = rows.Count == 1 && expected.All(pair => ReadInt(row, pair.Key) == pair.Value) && hashesMatch &&
                    Get(row, "xbe_sha256") == "5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B" &&
                    Get(row, "status") == "verified-static-eagl-parameter-and-indexed-draw-control" &&
                    Get(row, "draw_opcode_binding") == "verified-static-command26-conditional-indexed-submission" &&
                    Get(row, "runtime_selector") == "unresolved" &&
                    Get(row, "vertex_attribute_binding") == "not-proven" &&
                    Get(row, "full_rendering_binding") == "not-proven" &&
                    Get(row, "original_semantics") == "not-claimed";
        return new ProjectReadinessRow(
            "rendering-original-parameter-and-indexed-draw-control",
            valid ? "verified-static-binding" : "not-present-or-stale", valid,
            ReadInt(row, "fetch_base_record_matches"), 11818,
            "static XBE handler contracts reconcile all serialized parameter pairs; command 26 reaches indexed draw packet emission; command 9 resolves exact __VB base records",
            "resolve runtime selector, vertex attribute/UV layout, material state and independently validate full draw output before promoting original rendering",
            string.Join("; ", expected.Keys.Select(key => $"{key}={Get(row, key)}")) +
            $"; input_hashes_match={hashesMatch}; draw_opcode_binding={Get(row, "draw_opcode_binding")}; runtime_selector={Get(row, "runtime_selector")}; vertex_attribute_binding={Get(row, "vertex_attribute_binding")}; full_rendering_binding={Get(row, "full_rendering_binding")}; original_semantics={Get(row, "original_semantics")}");
    }

    internal static ProjectReadinessRow ReadIndexedDrawSourceControl(string reportDirectory)
    {
        var rows = ReadTsvRows(Path.Combine(reportDirectory, "eagl-indexed-draw-source-control-summary.tsv"));
        var row = rows.FirstOrDefault();
        var expected = new Dictionary<string, int>
        {
            ["draw_rows"] = 2364, ["verified_local_sources"] = 2363, ["unresolved_sources"] = 1,
            ["rejected_sources"] = 0, ["independent_index_bytes"] = 1739618,
            ["static_spans"] = 5, ["static_sites"] = 37, ["negative_controls"] = 4,
        };
        bool HashMatches(string file, string field)
        {
            var path = Path.Combine(reportDirectory, file);
            if (!File.Exists(path) || !IsSha256Hex(Get(row, field))) return false;
            using var input = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(input)) == Get(row, field);
        }
        var hashesMatch = HashMatches("eagl-indexed-draw-source-audit.tsv", "draw_report_sha256") &&
            HashMatches("eagl-render-parameter-audit.tsv", "parameter_report_sha256") &&
            HashMatches("eagl-indexed-draw-source-control.tsv", "static_report_sha256");
        var valid = rows.Count == 1 && expected.All(p => ReadInt(row, p.Key) == p.Value) && hashesMatch &&
            Get(row, "xbe_sha256") == "5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B" &&
            Get(row, "status") == "verified-independent-local-indexed-draw-source-control" &&
            Get(row, "source_binding") == "descriptor-local-count-and-index-pointer-to-command26" &&
            Get(row, "vertex_attributes") == "not-proven" && Get(row, "runtime_selector") == "unresolved" &&
            Get(row, "full_rendering_binding") == "not-proven" && Get(row, "original_semantics") == "not-claimed";
        return new ProjectReadinessRow("rendering-original-local-indexed-draw-source",
            valid ? "verified-static-binding" : "not-present-or-stale", valid,
            ReadInt(row, "verified_local_sources"), 2363,
            "descriptor-local count and pointer reach command 26; independent ISO extraction verifies all local unsigned 16-bit index payloads",
            "resolve the one missing descriptor count, runtime-selected buffer records, primitive mode, shader inputs and UV/material before original rendering integration",
            string.Join("; ", expected.Keys.Select(k => $"{k}={Get(row, k)}")) +
            $"; input_hashes_match={hashesMatch}; full_rendering_binding=not-proven; original_semantics=not-claimed");
    }

    internal static ProjectReadinessRow ReadModelMaterialReferenceControl(string reportDirectory)
    {
        var rows = ReadTsvRows(Path.Combine(reportDirectory, "eagl-model-material-reference-control-summary.tsv"));
        var row = rows.FirstOrDefault();
        var expected = new Dictionary<string, int> {
            ["model_rows"] = 2364, ["model_objects"] = 883, ["material_definitions"] = 3,
            ["declarations_matched"] = 2364, ["parent_1c_2c_same_reference_rows"] = 2364,
            ["skin_rows"] = 1416, ["cloth_rows"] = 594, ["eye_rows"] = 354,
            ["static_spans"] = 4, ["static_sites"] = 53, ["negative_controls"] = 16 };
        bool HashMatches(string file, string field)
        {
            var path = Path.Combine(reportDirectory, file);
            if (!File.Exists(path) || !IsSha256Hex(Get(row, field))) return false;
            using var input = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(input)) == Get(row, field);
        }
        var hashesMatch = HashMatches("eagl-model-material-reference-audit.tsv", "report_sha256") &&
            HashMatches("eagl-model-material-reference-control.tsv", "static_report_sha256");
        var valid = rows.Count == 1 && expected.All(p => ReadInt(row, p.Key) == p.Value) && hashesMatch &&
            Get(row, "source_path") == EaglMaterialDeclarationAudit.Source &&
            Get(row, "source_sha256") == EaglMaterialDeclarationAudit.SourceSha256 &&
            Get(row, "declaration_sha256") == EaglModelMaterialReferenceAudit.DeclarationHash &&
            Get(row, "xbe_sha256") == "5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B" &&
            Get(row, "status") == "verified-independent-model-material-reference-control" &&
            Get(row, "reference_scope") == "serialized-reference-to-exact-named-program-definition" &&
            Get(row, "runtime_loader_resolution") == "not-proven" && Get(row, "selected_buffers") == "not-proven" &&
            Get(row, "full_rendering_binding") == "not-proven" && Get(row, "original_semantics") == "not-claimed";
        return new ProjectReadinessRow("rendering-model-material-program-references",
            valid ? "verified-static-model-reference" : "not-present-or-stale", valid, ReadInt(row, "model_rows"), 2364,
            "independent fresh model/layer/descriptor enumeration verifies exact declaration bytes and explicit named VS/PS references to three material definitions",
            "prove runtime loader resolution, selected buffers, constants and effective draw installation before original rendering integration",
            string.Join("; ", expected.Keys.Select(k => $"{k}={Get(row, k)}")) +
            $"; input_hashes_match={hashesMatch}; runtime_loader_resolution=not-proven; selected_buffers=not-proven; full_rendering_binding=not-proven; original_semantics=not-claimed");
    }

    internal static ProjectReadinessRow ReadVertexProgramControl(string reportDirectory)
    {
        var rows = ReadTsvRows(Path.Combine(reportDirectory, "eagl-vertex-program-control-summary.tsv"));
        var row = rows.FirstOrDefault();
        var expected = new Dictionary<string, int> {
            ["instruction_count"] = 46, ["active_input_slot_references"] = 8, ["relative_constant_instructions"] = 6,
            ["output_write_instructions"] = 9, ["paired_instructions"] = 6,
            ["static_spans"] = 5, ["static_sites"] = 65, ["negative_controls"] = 18 };
        bool HashMatches(string file, string field)
        {
            var path = Path.Combine(reportDirectory, file);
            if (!File.Exists(path) || !IsSha256Hex(Get(row, field))) return false;
            using var input = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(input)) == Get(row, field);
        }
        var hashesMatch = HashMatches("eagl-vertex-program-audit.tsv", "report_sha256") &&
            HashMatches("eagl-vertex-program-audit-summary.tsv", "managed_summary_sha256") &&
            HashMatches("eagl-vertex-program-control.tsv", "static_report_sha256");
        var valid = rows.Count == 1 && expected.All(p => ReadInt(row, p.Key) == p.Value) && hashesMatch &&
            Get(row, "source_path") == EaglMaterialDeclarationAudit.Source &&
            Get(row, "source_sha256") == EaglMaterialDeclarationAudit.SourceSha256 &&
            Get(row, "vertex_program_sha256") == EaglVertexProgramAudit.ProgramSha256 &&
            Get(row, "xbe_sha256") == "5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B" &&
            Get(row, "active_input_registers") == "0,1,3,4" && Get(row, "active_output_addresses") == "0,3,4,9" &&
            Get(row, "status") == "verified-independent-vertex-program-opcode-operands-control" &&
            Get(row, "operand_scope") == "opcode-active-source-slots-not-component-liveness" &&
            Get(row, "parent_inheritance_binding") == "not-proven" && Get(row, "full_rendering_binding") == "not-proven" &&
            Get(row, "original_semantics") == "not-claimed";
        return new ProjectReadinessRow("rendering-vertex-program-opcode-operands",
            valid ? "verified-static-instruction-layout" : "not-present-or-stale", valid, ReadInt(row, "instruction_count"), 46,
            "fresh ISO and independent packed-bit decoder reconcile all 46 instruction rows, active source slots and unmodified DWORD upload",
            "prove serialized-parent resolution, effective draw installation, component dataflow and selected buffers before integrating original rendering",
            string.Join("; ", expected.Keys.Select(k => $"{k}={Get(row, k)}")) +
            $"; input_hashes_match={hashesMatch}; inputs=0,1,3,4; outputs=0,3,4,9; parent_inheritance_binding=not-proven; full_rendering_binding=not-proven; original_semantics=not-claimed");
    }

    internal static ProjectReadinessRow ReadMaterialDeclarationControl(string reportDirectory)
    {
        var rows = ReadTsvRows(Path.Combine(reportDirectory, "eagl-material-declaration-control-summary.tsv"));
        var row = rows.FirstOrDefault();
        var expected = new Dictionary<string, int> {
            ["declaration_elements"] = 5, ["declaration_bytes"] = 44,
            ["vertex_instruction_count"] = 46, ["vertex_program_bytes"] = 740,
            ["static_spans"] = 4, ["static_sites"] = 51, ["negative_controls"] = 7 };
        bool HashMatches(string file, string field)
        {
            var path = Path.Combine(reportDirectory, file);
            if (!File.Exists(path) || !IsSha256Hex(Get(row, field))) return false;
            using var input = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(input)) == Get(row, field);
        }
        var hashesMatch = HashMatches("eagl-material-declaration-audit.tsv", "declaration_report_sha256") &&
            HashMatches("eagl-material-declaration-control.tsv", "static_report_sha256");
        var valid = rows.Count == 1 && expected.All(p => ReadInt(row, p.Key) == p.Value) && hashesMatch &&
            Get(row, "source_path") == EaglMaterialDeclarationAudit.Source &&
            Get(row, "source_sha256") == EaglMaterialDeclarationAudit.SourceSha256 &&
            Get(row, "xbe_sha256") == "5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B" &&
            Get(row, "size_tables_address") == "0x0029C760" &&
            Get(row, "size_tables_sha256") == "2F6CF9E3BD99EB8EEB79080DD0867A4E0A68D14B7A3220A07BA704F8882FBB3D" &&
            Get(row, "status") == "verified-independent-material-declaration-and-vs-envelope-control" &&
            Get(row, "attribute_semantics") == "not-proven" && Get(row, "parent_inheritance_binding") == "not-proven" &&
            Get(row, "full_rendering_binding") == "not-proven" && Get(row, "original_semantics") == "not-claimed";
        return new ProjectReadinessRow("rendering-material-declaration-and-vs-envelope",
            valid ? "verified-static-layout" : "not-present-or-stale", valid, ReadInt(row, "declaration_elements"), 5,
            "fresh ISO extraction and static retail consumer validate five stream/register/format fields, byte widths and a 46-instruction VS envelope",
            "prove parent inheritance, shader input/output behavior and runtime-selected buffer state before assigning attributes or integrating original rendering",
            string.Join("; ", expected.Keys.Select(k => $"{k}={Get(row, k)}")) +
            $"; input_hashes_match={hashesMatch}; attribute_semantics=not-proven; parent_inheritance_binding=not-proven; full_rendering_binding=not-proven; original_semantics=not-claimed");
    }

    private static StaticRenderCommandDispatchEvidence ReadStaticRenderCommandDispatchEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\teagl_report_sha256\teagl_rows\tcomplete_command_stream_rows\tcommand_occurrences\tunique_observed_command_ids\tmapped_observed_command_ids\texecutable_dispatch_table_prefix_entries\tdistinct_dispatch_targets\tdispatch_table_base\tdispatch_table_file_offset\tfirst_non_executable_index\tfirst_non_executable_word\thandler_table_scan_capped\tdispatcher_instruction_addresses_validated\tstatus\tprovenance\tdispatch_binding\tsemantic_binding\toriginal_semantics";
        const string expectedXbeSha256 = "5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var xbeSha256 = Get(row, "xbe_sha256");
        var eaglReportSha256 = Get(row, "eagl_report_sha256");
        var eaglRows = ReadInt(row, "eagl_rows");
        var completeCommandStreamRows = ReadInt(row, "complete_command_stream_rows");
        var commandOccurrences = ReadInt(row, "command_occurrences");
        var uniqueObservedCommandIds = ReadInt(row, "unique_observed_command_ids");
        var mappedObservedCommandIds = ReadInt(row, "mapped_observed_command_ids");
        var executableDispatchTablePrefixEntries = ReadInt(row, "executable_dispatch_table_prefix_entries");
        var distinctDispatchTargets = ReadInt(row, "distinct_dispatch_targets");
        var dispatchTableBase = Get(row, "dispatch_table_base");
        var firstNonExecutableIndex = ReadInt(row, "first_non_executable_index");
        var firstNonExecutableWord = Get(row, "first_non_executable_word");
        var handlerTableScanCapped = Get(row, "handler_table_scan_capped");
        var dispatcherInstructionAddressesValidated = ReadInt(row, "dispatcher_instruction_addresses_validated");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var dispatchBinding = Get(row, "dispatch_binding");
        var semanticBinding = Get(row, "semantic_binding");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(xbeSha256, expectedXbeSha256, StringComparison.Ordinal) &&
                    eaglReportSha256.Length == 64 && eaglReportSha256.All(Uri.IsHexDigit) &&
                    eaglRows == 2_364 &&
                    completeCommandStreamRows == eaglRows &&
                    commandOccurrences == 36_779 &&
                    uniqueObservedCommandIds == 12 &&
                    mappedObservedCommandIds == uniqueObservedCommandIds &&
                    executableDispatchTablePrefixEntries == 42 &&
                    distinctDispatchTargets == 39 &&
                    string.Equals(dispatchTableBase, "0x0031A930", StringComparison.Ordinal) &&
                    firstNonExecutableIndex == executableDispatchTablePrefixEntries &&
                    string.Equals(firstNonExecutableWord, "0x00000001", StringComparison.Ordinal) &&
                    string.Equals(handlerTableScanCapped, "False", StringComparison.OrdinalIgnoreCase) &&
                    dispatcherInstructionAddressesValidated == 7 &&
                    string.Equals(status, "verified-static-eagl-command-id-dispatch-control", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(dispatchBinding, "verified-static-command-header-index-to-handler-table-slot", StringComparison.Ordinal) &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            xbeSha256,
            eaglReportSha256,
            eaglRows,
            completeCommandStreamRows,
            commandOccurrences,
            uniqueObservedCommandIds,
            mappedObservedCommandIds,
            executableDispatchTablePrefixEntries,
            distinctDispatchTargets,
            dispatchTableBase,
            firstNonExecutableIndex,
            firstNonExecutableWord,
            status,
            provenance,
            dispatchBinding,
            semanticBinding,
            originalSemantics);
    }

    private static StaticRenderIndirectIndexedTargetEvidence ReadStaticRenderIndirectIndexedTargetEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tindexed_table_rows\tcandidate_entry_occurrences\tdistinct_candidate_targets\tmapped_executable_targets\tdisassembled_candidate_targets\tret_terminated_candidate_targets\tprologue_like_candidate_targets\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var indexedTableRows = ReadInt(row, "indexed_table_rows");
        var candidateEntryOccurrences = ReadInt(row, "candidate_entry_occurrences");
        var distinctCandidateTargets = ReadInt(row, "distinct_candidate_targets");
        var mappedExecutableTargets = ReadInt(row, "mapped_executable_targets");
        var disassembledCandidateTargets = ReadInt(row, "disassembled_candidate_targets");
        var retTerminatedCandidateTargets = ReadInt(row, "ret_terminated_candidate_targets");
        var prologueLikeCandidateTargets = ReadInt(row, "prologue_like_candidate_targets");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-bounded-static-render-indirect-indexed-target-slices", StringComparison.Ordinal) &&
                    indexedTableRows > 0 &&
                    candidateEntryOccurrences > 0 &&
                    distinctCandidateTargets > 0 &&
                    mappedExecutableTargets == distinctCandidateTargets &&
                    disassembledCandidateTargets == mappedExecutableTargets &&
                    retTerminatedCandidateTargets >= 0 &&
                    retTerminatedCandidateTargets <= disassembledCandidateTargets &&
                    prologueLikeCandidateTargets >= 0 &&
                    prologueLikeCandidateTargets <= disassembledCandidateTargets &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            indexedTableRows,
            candidateEntryOccurrences,
            distinctCandidateTargets,
            mappedExecutableTargets,
            disassembledCandidateTargets,
            retTerminatedCandidateTargets,
            prologueLikeCandidateTargets,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderIndirectIndexedAccessorEvidence ReadStaticRenderIndirectIndexedAccessorEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tindexed_table_rows\tcandidate_entry_occurrences\tdistinct_candidate_targets\tmapped_executable_targets\tdisassembled_candidate_targets\tret_terminated_candidate_targets\tfallback_pop_pop_ret_shapes\tsingle_mov_memory_ret_shapes\tlea_push_call_ret_shapes\tother_shapes\tdistinct_memory_displacements\tdistinct_direct_call_targets\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var indexedTableRows = ReadInt(row, "indexed_table_rows");
        var candidateEntryOccurrences = ReadInt(row, "candidate_entry_occurrences");
        var distinctCandidateTargets = ReadInt(row, "distinct_candidate_targets");
        var mappedExecutableTargets = ReadInt(row, "mapped_executable_targets");
        var disassembledCandidateTargets = ReadInt(row, "disassembled_candidate_targets");
        var retTerminatedCandidateTargets = ReadInt(row, "ret_terminated_candidate_targets");
        var fallbackPopPopRetShapes = ReadInt(row, "fallback_pop_pop_ret_shapes");
        var singleMovMemoryRetShapes = ReadInt(row, "single_mov_memory_ret_shapes");
        var leaPushCallRetShapes = ReadInt(row, "lea_push_call_ret_shapes");
        var otherShapes = ReadInt(row, "other_shapes");
        var distinctMemoryDisplacements = ReadInt(row, "distinct_memory_displacements");
        var distinctDirectCallTargets = ReadInt(row, "distinct_direct_call_targets");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-bounded-static-render-indirect-indexed-accessor-shapes", StringComparison.Ordinal) &&
                    indexedTableRows > 0 &&
                    candidateEntryOccurrences > 0 &&
                    distinctCandidateTargets > 0 &&
                    mappedExecutableTargets == distinctCandidateTargets &&
                    disassembledCandidateTargets == mappedExecutableTargets &&
                    retTerminatedCandidateTargets == disassembledCandidateTargets &&
                    fallbackPopPopRetShapes >= 0 &&
                    singleMovMemoryRetShapes >= 0 &&
                    leaPushCallRetShapes >= 0 &&
                    otherShapes >= 0 &&
                    fallbackPopPopRetShapes + singleMovMemoryRetShapes + leaPushCallRetShapes + otherShapes == disassembledCandidateTargets &&
                    distinctMemoryDisplacements >= 0 &&
                    distinctDirectCallTargets >= 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            indexedTableRows,
            candidateEntryOccurrences,
            distinctCandidateTargets,
            mappedExecutableTargets,
            disassembledCandidateTargets,
            retTerminatedCandidateTargets,
            fallbackPopPopRetShapes,
            singleMovMemoryRetShapes,
            leaPushCallRetShapes,
            otherShapes,
            distinctMemoryDisplacements,
            distinctDirectCallTargets,
            status,
            provenance,
            originalSemantics);
    }

    private static CandidateFetchAssociationEvidence ReadCandidateFetchAssociationEvidence(string path)
    {
        const string expectedHeader =
            "model_archive_path\tcandidate_count\tcandidate_assets_loaded\tcandidate_assets_with_complete_five_slot_layouts\tprimitive_rows\tcomplete_five_slot_primitive_rows\tslot_rows\tvalid_slot_rows\trejected_candidates\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var candidateCount = ReadInt(row, "candidate_count");
        var candidateAssetsLoaded = ReadInt(row, "candidate_assets_loaded");
        var candidateAssetsWithCompleteFiveSlotLayouts = ReadInt(row, "candidate_assets_with_complete_five_slot_layouts");
        var primitiveRows = ReadInt(row, "primitive_rows");
        var completeFiveSlotPrimitiveRows = ReadInt(row, "complete_five_slot_primitive_rows");
        var slotRows = ReadInt(row, "slot_rows");
        var validSlotRows = ReadInt(row, "valid_slot_rows");
        var rejectedCandidates = ReadInt(row, "rejected_candidates");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, EaglCandidateFetchAssociationAudit.Status, StringComparison.Ordinal) &&
                    candidateCount > 0 &&
                    candidateAssetsLoaded == candidateCount &&
                    candidateAssetsWithCompleteFiveSlotLayouts > 0 &&
                    primitiveRows > 0 &&
                    completeFiveSlotPrimitiveRows > 0 &&
                    slotRows > 0 &&
                    validSlotRows == slotRows &&
                    rejectedCandidates == 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            candidateCount,
            candidateAssetsLoaded,
            candidateAssetsWithCompleteFiveSlotLayouts,
            primitiveRows,
            completeFiveSlotPrimitiveRows,
            slotRows,
            validSlotRows,
            rejectedCandidates,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalCandidateSlotProfileEvidence ReadOriginalCandidateSlotProfileEvidence(string path)
    {
        const string expectedHeader =
            "original_stream_report\tcandidate_fetch_report\toriginal_stream_rows\tcandidate_primitive_rows\tcandidate_complete_five_slot_rows\tcandidate_slot1_rows\tsymbol_match_rows\tobject_match_rows\tcategory11_rows\tfinite_float2_rows\tnear_unit_range_float2_rows\tstream_stride_match_rows\tbyte_length_match_rows\telement_count_match_rows\tcomplete_control_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var originalStreamRows = ReadInt(row, "original_stream_rows");
        var candidatePrimitiveRows = ReadInt(row, "candidate_primitive_rows");
        var candidateCompleteFiveSlotRows = ReadInt(row, "candidate_complete_five_slot_rows");
        var candidateSlot1Rows = ReadInt(row, "candidate_slot1_rows");
        var symbolMatchRows = ReadInt(row, "symbol_match_rows");
        var objectMatchRows = ReadInt(row, "object_match_rows");
        var category11Rows = ReadInt(row, "category11_rows");
        var finiteFloat2Rows = ReadInt(row, "finite_float2_rows");
        var nearUnitRangeFloat2Rows = ReadInt(row, "near_unit_range_float2_rows");
        var streamStrideMatchRows = ReadInt(row, "stream_stride_match_rows");
        var byteLengthMatchRows = ReadInt(row, "byte_length_match_rows");
        var elementCountMatchRows = ReadInt(row, "element_count_match_rows");
        var completeControlRows = ReadInt(row, "complete_control_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, EaglOriginalCandidateSlotProfileAudit.Status, StringComparison.Ordinal) &&
                    originalStreamRows == 11820 &&
                    candidatePrimitiveRows == 218 &&
                    candidateCompleteFiveSlotRows == 216 &&
                    candidateSlot1Rows == 216 &&
                    symbolMatchRows == 216 &&
                    objectMatchRows == 216 &&
                    category11Rows == 216 &&
                    finiteFloat2Rows == 216 &&
                    nearUnitRangeFloat2Rows == 216 &&
                    streamStrideMatchRows == 216 &&
                    byteLengthMatchRows == 216 &&
                    elementCountMatchRows == 216 &&
                    completeControlRows == 216 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            originalStreamRows,
            candidatePrimitiveRows,
            candidateCompleteFiveSlotRows,
            candidateSlot1Rows,
            symbolMatchRows,
            objectMatchRows,
            category11Rows,
            finiteFloat2Rows,
            nearUnitRangeFloat2Rows,
            streamStrideMatchRows,
            byteLengthMatchRows,
            elementCountMatchRows,
            completeControlRows,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalCandidateRawStreamIdentityEvidence ReadOriginalCandidateRawStreamIdentityEvidence(string path)
    {
        const string expectedHeader =
            "model_archive_path\tcandidate_fetch_report\tcandidate_object_count\tcandidate_primitive_rows\tcandidate_complete_five_slot_rows\tcandidate_slot_rows\traw_objects_loaded\traw_streams_loaded\traw_valid_slot_rows\traw_metadata_exact_rows\traw_payload_hash_rows\tcomplete_identity_rows\texpected_unmaterialized_rows\trejected_objects\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var candidateObjectCount = ReadInt(row, "candidate_object_count");
        var candidatePrimitiveRows = ReadInt(row, "candidate_primitive_rows");
        var candidateCompleteFiveSlotRows = ReadInt(row, "candidate_complete_five_slot_rows");
        var candidateSlotRows = ReadInt(row, "candidate_slot_rows");
        var rawObjectsLoaded = ReadInt(row, "raw_objects_loaded");
        var rawStreamsLoaded = ReadInt(row, "raw_streams_loaded");
        var rawValidSlotRows = ReadInt(row, "raw_valid_slot_rows");
        var rawMetadataExactRows = ReadInt(row, "raw_metadata_exact_rows");
        var rawPayloadHashRows = ReadInt(row, "raw_payload_hash_rows");
        var completeIdentityRows = ReadInt(row, "complete_identity_rows");
        var expectedUnmaterializedRows = ReadInt(row, "expected_unmaterialized_rows");
        var rejectedObjects = ReadInt(row, "rejected_objects");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, EaglOriginalCandidateRawStreamIdentityAudit.Status, StringComparison.Ordinal) &&
                    candidateObjectCount == 24 &&
                    candidatePrimitiveRows == 218 &&
                    candidateCompleteFiveSlotRows == 216 &&
                    candidateSlotRows == 1080 &&
                    rawObjectsLoaded == 24 &&
                    rawStreamsLoaded == 1090 &&
                    rawValidSlotRows == 1080 &&
                    rawMetadataExactRows == 1080 &&
                    rawPayloadHashRows == 1080 &&
                    completeIdentityRows == 216 &&
                    expectedUnmaterializedRows == 2 &&
                    rejectedObjects == 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            candidateObjectCount,
            candidatePrimitiveRows,
            candidateCompleteFiveSlotRows,
            candidateSlotRows,
            rawObjectsLoaded,
            rawStreamsLoaded,
            rawValidSlotRows,
            rawMetadataExactRows,
            rawPayloadHashRows,
            completeIdentityRows,
            expectedUnmaterializedRows,
            rejectedObjects,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalCandidateSlotCategoryControlEvidence ReadOriginalCandidateSlotCategoryControlEvidence(string path)
    {
        const string expectedHeader =
            "candidate_rows\traw_identity_rows\tcandidate_object_rows\traw_identity_object_rows\tjoined_rows\tcandidate_complete_rows\traw_identity_complete_rows\texpected_unmaterialized_rows\tcandidate_slot_rows\tsymbol_format_rows\tcategory_exact_rows\tordinal_exact_rows\tcomplete_control_rows\tmissing_candidate_keys\tmissing_raw_identity_keys\tstatus\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var candidateRows = ReadInt(row, "candidate_rows");
        var rawIdentityRows = ReadInt(row, "raw_identity_rows");
        var candidateObjectRows = ReadInt(row, "candidate_object_rows");
        var rawIdentityObjectRows = ReadInt(row, "raw_identity_object_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var candidateCompleteRows = ReadInt(row, "candidate_complete_rows");
        var rawIdentityCompleteRows = ReadInt(row, "raw_identity_complete_rows");
        var expectedUnmaterializedRows = ReadInt(row, "expected_unmaterialized_rows");
        var candidateSlotRows = ReadInt(row, "candidate_slot_rows");
        var symbolFormatRows = ReadInt(row, "symbol_format_rows");
        var categoryExactRows = ReadInt(row, "category_exact_rows");
        var ordinalExactRows = ReadInt(row, "ordinal_exact_rows");
        var completeControlRows = ReadInt(row, "complete_control_rows");
        var missingCandidateKeys = ReadInt(row, "missing_candidate_keys");
        var missingRawIdentityKeys = ReadInt(row, "missing_raw_identity_keys");
        var status = Get(row, "status");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-candidate-slot-category-ordinal-control", StringComparison.Ordinal) &&
                    candidateRows == 218 &&
                    rawIdentityRows == 218 &&
                    candidateObjectRows == 24 &&
                    rawIdentityObjectRows == 24 &&
                    joinedRows == 218 &&
                    candidateCompleteRows == 216 &&
                    rawIdentityCompleteRows == 216 &&
                    expectedUnmaterializedRows == 2 &&
                    candidateSlotRows == 1080 &&
                    symbolFormatRows == 1080 &&
                    categoryExactRows == 1080 &&
                    ordinalExactRows == 1080 &&
                    completeControlRows == 218 &&
                    missingCandidateKeys == 0 &&
                    missingRawIdentityKeys == 0 &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            candidateRows,
            rawIdentityRows,
            candidateObjectRows,
            rawIdentityObjectRows,
            joinedRows,
            candidateCompleteRows,
            rawIdentityCompleteRows,
            expectedUnmaterializedRows,
            candidateSlotRows,
            symbolFormatRows,
            categoryExactRows,
            ordinalExactRows,
            completeControlRows,
            missingCandidateKeys,
            missingRawIdentityKeys,
            status,
            originalSemantics);
    }

    private static OriginalRenderChainEvidence ReadOriginalRenderChainEvidence(string path)
    {
        const string expectedHeader =
            "model_archive_path\tmodel_package_candidates\tmodel_packages_loaded\tobject_candidates\tobjects_loaded\tmodel_symbol_rows\tbounds_symbol_rows\trender_method_symbol_rows\tvertex_buffer_symbol_rows\tvalidated_vertex_stream_rows\tobjects_with_bounds\tobjects_with_validated_vertex_streams\tprimitive_rows\tvalid_chain_rows\tcomplete_command_streams\tstate_relocation_rows\tshapename_state_rows\tprimitive_type_state_rows\tfetch_slot_rows\tcomplete_five_slot_fetch_rows\tstate_fetch_overlap_rows\tstate_shapename_primitive_type_overlap_rows\trejected_packages\trejected_objects\tstatus\tprovenance\toriginal_semantics\tstate_fetch_control_status\tvertex_record_control_status";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var modelPackageCandidates = ReadInt(row, "model_package_candidates");
        var modelPackagesLoaded = ReadInt(row, "model_packages_loaded");
        var objectCandidates = ReadInt(row, "object_candidates");
        var objectsLoaded = ReadInt(row, "objects_loaded");
        var modelSymbolRows = ReadInt(row, "model_symbol_rows");
        var boundsSymbolRows = ReadInt(row, "bounds_symbol_rows");
        var renderMethodSymbolRows = ReadInt(row, "render_method_symbol_rows");
        var vertexBufferSymbolRows = ReadInt(row, "vertex_buffer_symbol_rows");
        var validatedVertexStreamRows = ReadInt(row, "validated_vertex_stream_rows");
        var objectsWithBounds = ReadInt(row, "objects_with_bounds");
        var objectsWithValidatedVertexStreams = ReadInt(row, "objects_with_validated_vertex_streams");
        var primitiveRows = ReadInt(row, "primitive_rows");
        var validChainRows = ReadInt(row, "valid_chain_rows");
        var completeCommandStreams = ReadInt(row, "complete_command_streams");
        var stateRelocationRows = ReadInt(row, "state_relocation_rows");
        var shapeNameStateRows = ReadInt(row, "shapename_state_rows");
        var primitiveTypeStateRows = ReadInt(row, "primitive_type_state_rows");
        var fetchSlotRows = ReadInt(row, "fetch_slot_rows");
        var completeFiveSlotFetchRows = ReadInt(row, "complete_five_slot_fetch_rows");
        var stateFetchOverlapRows = ReadInt(row, "state_fetch_overlap_rows");
        var stateShapePrimitiveTypeOverlapRows = ReadInt(row, "state_shapename_primitive_type_overlap_rows");
        var rejectedPackages = ReadInt(row, "rejected_packages");
        var rejectedObjects = ReadInt(row, "rejected_objects");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var stateFetchControlStatus = Get(row, "state_fetch_control_status");
        var vertexRecordControlStatus = Get(row, "vertex_record_control_status");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, EaglOriginalRenderChainAudit.Status, StringComparison.Ordinal) &&
                    modelPackageCandidates > 0 &&
                    modelPackagesLoaded == modelPackageCandidates &&
                    objectCandidates > 0 &&
                    objectsLoaded > 0 &&
                    modelSymbolRows > 0 &&
                    boundsSymbolRows >= 0 &&
                    renderMethodSymbolRows >= 0 &&
                    vertexBufferSymbolRows > 0 &&
                    validatedVertexStreamRows > 0 &&
                    objectsWithBounds > 0 &&
                    objectsWithValidatedVertexStreams > 0 &&
                    primitiveRows > 0 &&
                    validChainRows == primitiveRows &&
                    completeCommandStreams == primitiveRows &&
                    stateRelocationRows >= 0 &&
                    shapeNameStateRows >= 0 &&
                    primitiveTypeStateRows >= 0 &&
                    fetchSlotRows >= 0 &&
                    completeFiveSlotFetchRows >= 0 &&
                    stateFetchOverlapRows >= 0 &&
                    stateShapePrimitiveTypeOverlapRows >= 0 &&
                    rejectedPackages == 0 &&
                    rejectedObjects == 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal) &&
                    string.Equals(stateFetchControlStatus, EaglOriginalRenderChainAudit.StateFetchControlStatus, StringComparison.Ordinal) &&
                    string.Equals(vertexRecordControlStatus, EaglOriginalRenderChainAudit.VertexRecordControlStatus, StringComparison.Ordinal);
        return new(
            valid,
            modelPackageCandidates,
            modelPackagesLoaded,
            objectCandidates,
            objectsLoaded,
            modelSymbolRows,
            boundsSymbolRows,
            renderMethodSymbolRows,
            vertexBufferSymbolRows,
            validatedVertexStreamRows,
            objectsWithBounds,
            objectsWithValidatedVertexStreams,
            primitiveRows,
            validChainRows,
            completeCommandStreams,
            stateRelocationRows,
            shapeNameStateRows,
            primitiveTypeStateRows,
            fetchSlotRows,
            completeFiveSlotFetchRows,
            stateFetchOverlapRows,
            stateShapePrimitiveTypeOverlapRows,
            rejectedPackages,
            rejectedObjects,
            status,
            provenance,
            originalSemantics,
            stateFetchControlStatus,
            vertexRecordControlStatus);
    }

    private static OriginalVertexRecordAssociationEvidence ReadOriginalVertexRecordAssociationEvidence(string path)
    {
        const string expectedHeader =
            "model_archive_path\tmodel_package_candidates\tmodel_packages_loaded\tobject_candidates\tobjects_loaded\tvalidated_vertex_stream_rows\tobjects_with_validated_vertex_streams\tprimitive_rows\trows_with_opaque_ordinal_records\trows_with_unique_opaque_sequences\tcomplete_five_slot_fetch_rows\trejected_packages\trejected_objects\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var modelPackageCandidates = ReadInt(row, "model_package_candidates");
        var modelPackagesLoaded = ReadInt(row, "model_packages_loaded");
        var objectCandidates = ReadInt(row, "object_candidates");
        var objectsLoaded = ReadInt(row, "objects_loaded");
        var validatedVertexStreamRows = ReadInt(row, "validated_vertex_stream_rows");
        var objectsWithValidatedVertexStreams = ReadInt(row, "objects_with_validated_vertex_streams");
        var primitiveRows = ReadInt(row, "primitive_rows");
        var rowsWithOpaqueOrdinalRecords = ReadInt(row, "rows_with_opaque_ordinal_records");
        var rowsWithUniqueOpaqueSequences = ReadInt(row, "rows_with_unique_opaque_sequences");
        var completeFiveSlotFetchRows = ReadInt(row, "complete_five_slot_fetch_rows");
        var rejectedPackages = ReadInt(row, "rejected_packages");
        var rejectedObjects = ReadInt(row, "rejected_objects");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, EaglOriginalVertexRecordAssociationAudit.Status, StringComparison.Ordinal) &&
                    modelPackageCandidates > 0 &&
                    modelPackagesLoaded == modelPackageCandidates &&
                    objectCandidates > 0 &&
                    objectsLoaded > 0 &&
                    validatedVertexStreamRows > 0 &&
                    objectsWithValidatedVertexStreams > 0 &&
                    primitiveRows > 0 &&
                    rowsWithOpaqueOrdinalRecords > 0 &&
                    rowsWithUniqueOpaqueSequences > 0 &&
                    completeFiveSlotFetchRows > 0 &&
                    rowsWithUniqueOpaqueSequences <= completeFiveSlotFetchRows &&
                    rejectedPackages == 0 &&
                    rejectedObjects == 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            modelPackageCandidates,
            modelPackagesLoaded,
            objectCandidates,
            objectsLoaded,
            validatedVertexStreamRows,
            objectsWithValidatedVertexStreams,
            primitiveRows,
            rowsWithOpaqueOrdinalRecords,
            rowsWithUniqueOpaqueSequences,
            completeFiveSlotFetchRows,
            rejectedPackages,
            rejectedObjects,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalVertexRecordAssociationControlEvidence ReadOriginalVertexRecordAssociationControlEvidence(string path)
    {
        const string expectedHeader =
            "source_report\tinput_rows\tobject_rows\tinput_unique_rows\trecomputed_exact_rows\treverse_stride_match_rows\tshifted_ordinal_match_rows\tcomplete_five_slot_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var inputRows = ReadInt(row, "input_rows");
        var objectRows = ReadInt(row, "object_rows");
        var inputUniqueRows = ReadInt(row, "input_unique_rows");
        var recomputedExactRows = ReadInt(row, "recomputed_exact_rows");
        var reverseStrideMatchRows = ReadInt(row, "reverse_stride_match_rows");
        var shiftedOrdinalMatchRows = ReadInt(row, "shifted_ordinal_match_rows");
        var completeFiveSlotRows = ReadInt(row, "complete_five_slot_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-opaque-vertex-record-association-control", StringComparison.Ordinal) &&
                    inputRows > 0 &&
                    objectRows > 0 &&
                    inputUniqueRows > 0 &&
                    recomputedExactRows == inputUniqueRows &&
                    reverseStrideMatchRows == 0 &&
                    shiftedOrdinalMatchRows == 0 &&
                    completeFiveSlotRows > 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            inputRows,
            objectRows,
            inputUniqueRows,
            recomputedExactRows,
            reverseStrideMatchRows,
            shiftedOrdinalMatchRows,
            completeFiveSlotRows,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalVertexPayloadEvidence ReadOriginalVertexPayloadEvidence(string path)
    {
        const string expectedHeader =
            "model_archive_path\tmodel_package_candidates\tmodel_packages_loaded\tobject_candidates\tobjects_loaded\tvalidated_vertex_stream_rows\tobjects_with_validated_vertex_streams\tobjects_with_finite_packed_float_profiles\tobjects_with_finite_float2_profiles\tobjects_with_declared_bounds_containment\tpayload_rows\tobjects_with_distinct_payload_ranges\tobjects_with_overlapping_payload_ranges\trejected_packages\trejected_objects\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var modelPackageCandidates = ReadInt(row, "model_package_candidates");
        var modelPackagesLoaded = ReadInt(row, "model_packages_loaded");
        var objectCandidates = ReadInt(row, "object_candidates");
        var objectsLoaded = ReadInt(row, "objects_loaded");
        var validatedVertexStreamRows = ReadInt(row, "validated_vertex_stream_rows");
        var objectsWithValidatedVertexStreams = ReadInt(row, "objects_with_validated_vertex_streams");
        var objectsWithFinitePackedFloatProfiles = ReadInt(row, "objects_with_finite_packed_float_profiles");
        var objectsWithFiniteFloat2Profiles = ReadInt(row, "objects_with_finite_float2_profiles");
        var objectsWithDeclaredBoundsContainment = ReadInt(row, "objects_with_declared_bounds_containment");
        var payloadRows = ReadInt(row, "payload_rows");
        var objectsWithDistinctPayloadRanges = ReadInt(row, "objects_with_distinct_payload_ranges");
        var objectsWithOverlappingPayloadRanges = ReadInt(row, "objects_with_overlapping_payload_ranges");
        var rejectedPackages = ReadInt(row, "rejected_packages");
        var rejectedObjects = ReadInt(row, "rejected_objects");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, EaglOriginalVertexPayloadAudit.Status, StringComparison.Ordinal) &&
                    modelPackageCandidates > 0 &&
                    modelPackagesLoaded == modelPackageCandidates &&
                    objectCandidates > 0 &&
                    objectsLoaded > 0 &&
                    validatedVertexStreamRows > 0 &&
                    objectsWithValidatedVertexStreams == objectsLoaded &&
                    objectsWithFinitePackedFloatProfiles > 0 &&
                    objectsWithFiniteFloat2Profiles > 0 &&
                    objectsWithDeclaredBoundsContainment > 0 &&
                    payloadRows == objectsLoaded &&
                    objectsWithDistinctPayloadRanges == objectsLoaded &&
                    objectsWithOverlappingPayloadRanges >= 0 &&
                    rejectedPackages == 0 &&
                    rejectedObjects == 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            modelPackageCandidates,
            modelPackagesLoaded,
            objectCandidates,
            objectsLoaded,
            validatedVertexStreamRows,
            objectsWithValidatedVertexStreams,
            objectsWithFinitePackedFloatProfiles,
            objectsWithFiniteFloat2Profiles,
            objectsWithDeclaredBoundsContainment,
            payloadRows,
            objectsWithDistinctPayloadRanges,
            objectsWithOverlappingPayloadRanges,
            rejectedPackages,
            rejectedObjects,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalVertexPayloadControlEvidence ReadOriginalVertexPayloadControlEvidence(string path)
    {
        const string expectedHeader =
            "source_report\tinput_rows\tobject_rows\tnumeric_signature_match_rows\tpayload_range_metadata_valid_rows\tinput_status_valid_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var inputRows = ReadInt(row, "input_rows");
        var objectRows = ReadInt(row, "object_rows");
        var numericSignatureMatchRows = ReadInt(row, "numeric_signature_match_rows");
        var payloadRangeMetadataValidRows = ReadInt(row, "payload_range_metadata_valid_rows");
        var inputStatusValidRows = ReadInt(row, "input_status_valid_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-numeric-payload-control", StringComparison.Ordinal) &&
                    inputRows > 0 &&
                    objectRows > 0 &&
                    numericSignatureMatchRows == inputRows &&
                    payloadRangeMetadataValidRows == inputRows &&
                    inputStatusValidRows == inputRows &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            inputRows,
            objectRows,
            numericSignatureMatchRows,
            payloadRangeMetadataValidRows,
            inputStatusValidRows,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalNumericStreamAmbiguityEvidence ReadOriginalNumericStreamAmbiguityEvidence(string path)
    {
        const string expectedHeader =
            "source_report\tinput_rows\tvalid_input_rows\tobjects_with_no_bounds_match\tobjects_with_unique_bounds_match\tobjects_with_multiple_bounds_matches\tobjects_with_non_unique_or_missing_match\tbounds_match_cardinality_histogram\tsemantic_binding\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var inputRows = ReadInt(row, "input_rows");
        var validInputRows = ReadInt(row, "valid_input_rows");
        var objectsWithNoBoundsMatch = ReadInt(row, "objects_with_no_bounds_match");
        var objectsWithUniqueBoundsMatch = ReadInt(row, "objects_with_unique_bounds_match");
        var objectsWithMultipleBoundsMatches = ReadInt(row, "objects_with_multiple_bounds_matches");
        var objectsWithNonUniqueOrMissingMatch = ReadInt(row, "objects_with_non_unique_or_missing_match");
        var boundsMatchCardinalityHistogram = Get(row, "bounds_match_cardinality_histogram");
        var semanticBinding = Get(row, "semantic_binding");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-numeric-stream-ambiguity-control", StringComparison.Ordinal) &&
                    inputRows > 0 &&
                    validInputRows == inputRows &&
                    objectsWithNoBoundsMatch >= 0 &&
                    objectsWithUniqueBoundsMatch >= 0 &&
                    objectsWithMultipleBoundsMatches >= 0 &&
                    objectsWithNonUniqueOrMissingMatch == objectsWithNoBoundsMatch + objectsWithMultipleBoundsMatches &&
                    !string.IsNullOrWhiteSpace(boundsMatchCardinalityHistogram) &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            inputRows,
            validInputRows,
            objectsWithNoBoundsMatch,
            objectsWithUniqueBoundsMatch,
            objectsWithMultipleBoundsMatches,
            objectsWithNonUniqueOrMissingMatch,
            boundsMatchCardinalityHistogram,
            semanticBinding,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalBoundsConsistentRecordEvidence ReadOriginalBoundsConsistentRecordEvidence(string path)
    {
        const string expectedHeader =
            "source_report\tinput_rows\tobject_ordinal_groups\tcomplete_five_stream_groups\tcategory_sequence_valid_groups\tvalid_stream_rows\tfirst_raw_stride12_groups\tfirst_raw_bounds_match_groups\tfirst_raw_no_bounds_match_groups\tlater_raw_bounds_match_groups\tall_bounds_matches_are_first_raw\tfirst_raw_category_histogram\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var inputRows = ReadInt(row, "input_rows");
        var objectOrdinalGroups = ReadInt(row, "object_ordinal_groups");
        var completeFiveStreamGroups = ReadInt(row, "complete_five_stream_groups");
        var categorySequenceValidGroups = ReadInt(row, "category_sequence_valid_groups");
        var validStreamRows = ReadInt(row, "valid_stream_rows");
        var firstRawStride12Groups = ReadInt(row, "first_raw_stride12_groups");
        var firstRawBoundsMatchGroups = ReadInt(row, "first_raw_bounds_match_groups");
        var firstRawNoBoundsMatchGroups = ReadInt(row, "first_raw_no_bounds_match_groups");
        var laterRawBoundsMatchGroups = ReadInt(row, "later_raw_bounds_match_groups");
        var allBoundsMatchesAreFirstRaw = Get(row, "all_bounds_matches_are_first_raw");
        var firstRawCategoryHistogram = Get(row, "first_raw_category_histogram");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-bounds-consistent-record-control", StringComparison.Ordinal) &&
                    inputRows > 0 &&
                    objectOrdinalGroups > 0 &&
                    completeFiveStreamGroups == objectOrdinalGroups &&
                    categorySequenceValidGroups == objectOrdinalGroups &&
                    validStreamRows == inputRows &&
                    firstRawStride12Groups == objectOrdinalGroups &&
                    firstRawBoundsMatchGroups > 0 &&
                    firstRawNoBoundsMatchGroups >= 0 &&
                    laterRawBoundsMatchGroups == 0 &&
                    string.Equals(allBoundsMatchesAreFirstRaw, "True", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(firstRawCategoryHistogram) &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            inputRows,
            objectOrdinalGroups,
            completeFiveStreamGroups,
            categorySequenceValidGroups,
            validStreamRows,
            firstRawStride12Groups,
            firstRawBoundsMatchGroups,
            firstRawNoBoundsMatchGroups,
            laterRawBoundsMatchGroups,
            allBoundsMatchesAreFirstRaw,
            firstRawCategoryHistogram,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalBoundsFetchSlotJoinEvidence ReadOriginalBoundsFetchSlotJoinEvidence(string path)
    {
        const string expectedHeader =
            "numeric_rows\ttopology_rows\tjoined_rows\tmissing_numeric_keys\tmissing_topology_keys\tobject_ordinal_groups\tcomplete_five_stream_groups\tidentity_join_rows\tnumeric_input_status_valid_rows\ttopology_input_status_valid_rows\tfirst_raw_fetch_slot_zero_groups\tfirst_raw_stride_match_groups\tfirst_raw_bounds_match_groups\tfirst_raw_no_bounds_match_groups\tlater_raw_bounds_match_groups\tincomplete_fetch_slot_groups\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var numericRows = ReadInt(row, "numeric_rows");
        var topologyRows = ReadInt(row, "topology_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var missingNumericKeys = ReadInt(row, "missing_numeric_keys");
        var missingTopologyKeys = ReadInt(row, "missing_topology_keys");
        var objectOrdinalGroups = ReadInt(row, "object_ordinal_groups");
        var completeFiveStreamGroups = ReadInt(row, "complete_five_stream_groups");
        var identityJoinRows = ReadInt(row, "identity_join_rows");
        var numericInputStatusValidRows = ReadInt(row, "numeric_input_status_valid_rows");
        var topologyInputStatusValidRows = ReadInt(row, "topology_input_status_valid_rows");
        var firstRawFetchSlotZeroGroups = ReadInt(row, "first_raw_fetch_slot_zero_groups");
        var firstRawStrideMatchGroups = ReadInt(row, "first_raw_stride_match_groups");
        var firstRawBoundsMatchGroups = ReadInt(row, "first_raw_bounds_match_groups");
        var firstRawNoBoundsMatchGroups = ReadInt(row, "first_raw_no_bounds_match_groups");
        var laterRawBoundsMatchGroups = ReadInt(row, "later_raw_bounds_match_groups");
        var incompleteFetchSlotGroups = ReadInt(row, "incomplete_fetch_slot_groups");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-bounds-fetch-slot-join-control", StringComparison.Ordinal) &&
                    numericRows > 0 &&
                    topologyRows == numericRows &&
                    joinedRows == numericRows &&
                    missingNumericKeys == 0 &&
                    missingTopologyKeys == 0 &&
                    objectOrdinalGroups > 0 &&
                    completeFiveStreamGroups == objectOrdinalGroups &&
                    identityJoinRows == joinedRows &&
                    numericInputStatusValidRows == numericRows &&
                    topologyInputStatusValidRows == topologyRows &&
                    firstRawFetchSlotZeroGroups == objectOrdinalGroups &&
                    firstRawStrideMatchGroups == objectOrdinalGroups &&
                    firstRawBoundsMatchGroups > 0 &&
                    firstRawNoBoundsMatchGroups >= 0 &&
                    laterRawBoundsMatchGroups == 0 &&
                    incompleteFetchSlotGroups >= 0 &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            numericRows,
            topologyRows,
            joinedRows,
            missingNumericKeys,
            missingTopologyKeys,
            objectOrdinalGroups,
            completeFiveStreamGroups,
            identityJoinRows,
            numericInputStatusValidRows,
            topologyInputStatusValidRows,
            firstRawFetchSlotZeroGroups,
            firstRawStrideMatchGroups,
            firstRawBoundsMatchGroups,
            firstRawNoBoundsMatchGroups,
            laterRawBoundsMatchGroups,
            incompleteFetchSlotGroups,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalNumericSlotRoleDiscriminationEvidence ReadOriginalNumericSlotRoleDiscriminationEvidence(string path)
    {
        const string expectedHeader =
            "numeric_rows\ttopology_rows\tjoined_rows\tper_primitive_slot_rows\tmissing_numeric_keys\tmissing_topology_keys\tduplicate_numeric_keys\tduplicate_topology_keys\tprimitive_groups\tgroups_with_five_rows\tcomplete_five_slot_groups\tcomplete_groups_with_unique_near_unit_float2\tcomplete_groups_with_near_unit_float2\tcomplete_groups_with_slot1_near_unit_float2\tcomplete_groups_with_other_slot_near_unit_float2\tcomplete_groups_with_slot0_bounds_match\tcomplete_groups_with_no_slot0_bounds_match\tincomplete_five_slot_groups\tnumeric_input_status_valid_rows\ttopology_input_status_valid_rows\tslot_row_count_histogram\tnear_unit_float2_slot_histogram\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var numericRows = ReadInt(row, "numeric_rows");
        var topologyRows = ReadInt(row, "topology_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var perPrimitiveSlotRows = ReadInt(row, "per_primitive_slot_rows");
        var missingNumericKeys = ReadInt(row, "missing_numeric_keys");
        var missingTopologyKeys = ReadInt(row, "missing_topology_keys");
        var duplicateNumericKeys = ReadInt(row, "duplicate_numeric_keys");
        var duplicateTopologyKeys = ReadInt(row, "duplicate_topology_keys");
        var primitiveGroups = ReadInt(row, "primitive_groups");
        var groupsWithFiveRows = ReadInt(row, "groups_with_five_rows");
        var completeFiveSlotGroups = ReadInt(row, "complete_five_slot_groups");
        var completeGroupsWithUniqueNearUnitFloat2 = ReadInt(row, "complete_groups_with_unique_near_unit_float2");
        var completeGroupsWithNearUnitFloat2 = ReadInt(row, "complete_groups_with_near_unit_float2");
        var completeGroupsWithSlot1NearUnitFloat2 = ReadInt(row, "complete_groups_with_slot1_near_unit_float2");
        var completeGroupsWithOtherSlotNearUnitFloat2 = ReadInt(row, "complete_groups_with_other_slot_near_unit_float2");
        var completeGroupsWithSlot0BoundsMatch = ReadInt(row, "complete_groups_with_slot0_bounds_match");
        var completeGroupsWithNoSlot0BoundsMatch = ReadInt(row, "complete_groups_with_no_slot0_bounds_match");
        var incompleteFiveSlotGroups = ReadInt(row, "incomplete_five_slot_groups");
        var numericInputStatusValidRows = ReadInt(row, "numeric_input_status_valid_rows");
        var topologyInputStatusValidRows = ReadInt(row, "topology_input_status_valid_rows");
        var slotRowCountHistogram = Get(row, "slot_row_count_histogram");
        var nearUnitFloat2SlotHistogram = Get(row, "near_unit_float2_slot_histogram");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-numeric-slot-role-discrimination-control", StringComparison.Ordinal) &&
                    numericRows > 0 &&
                    topologyRows == numericRows &&
                    joinedRows == numericRows &&
                    perPrimitiveSlotRows == joinedRows &&
                    missingNumericKeys == 0 &&
                    missingTopologyKeys == 0 &&
                    duplicateNumericKeys == 0 &&
                    duplicateTopologyKeys == 0 &&
                    primitiveGroups > 0 &&
                    groupsWithFiveRows == primitiveGroups &&
                    completeFiveSlotGroups > 0 &&
                    completeGroupsWithUniqueNearUnitFloat2 == completeFiveSlotGroups &&
                    completeGroupsWithNearUnitFloat2 == completeFiveSlotGroups &&
                    completeGroupsWithSlot1NearUnitFloat2 == completeFiveSlotGroups &&
                    completeGroupsWithOtherSlotNearUnitFloat2 == 0 &&
                    completeGroupsWithSlot0BoundsMatch > 0 &&
                    completeGroupsWithNoSlot0BoundsMatch >= 0 &&
                    incompleteFiveSlotGroups >= 0 &&
                    numericInputStatusValidRows == numericRows &&
                    topologyInputStatusValidRows == topologyRows &&
                    !string.IsNullOrWhiteSpace(slotRowCountHistogram) &&
                    !string.IsNullOrWhiteSpace(nearUnitFloat2SlotHistogram) &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            numericRows,
            topologyRows,
            joinedRows,
            perPrimitiveSlotRows,
            missingNumericKeys,
            missingTopologyKeys,
            duplicateNumericKeys,
            duplicateTopologyKeys,
            primitiveGroups,
            groupsWithFiveRows,
            completeFiveSlotGroups,
            completeGroupsWithUniqueNearUnitFloat2,
            completeGroupsWithNearUnitFloat2,
            completeGroupsWithSlot1NearUnitFloat2,
            completeGroupsWithOtherSlotNearUnitFloat2,
            completeGroupsWithSlot0BoundsMatch,
            completeGroupsWithNoSlot0BoundsMatch,
            incompleteFiveSlotGroups,
            numericInputStatusValidRows,
            topologyInputStatusValidRows,
            slotRowCountHistogram,
            nearUnitFloat2SlotHistogram,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalStateNumericSlotCooccurrenceEvidence ReadOriginalStateNumericSlotCooccurrenceEvidence(string path)
    {
        const string expectedHeader =
            "state_rows\trole_rows\tjoined_rows\tmissing_state_keys\tmissing_role_keys\tduplicate_state_keys\tduplicate_role_keys\tstate_input_status_valid_rows\trole_input_status_valid_rows\tprimitive_groups\tgroups_with_five_role_rows\tcomplete_five_slot_groups\tstate_relocation_rows\tshapename_state_rows\tprimitive_type_state_rows\texact_adjacency_stream_rows\tcomplete_groups_with_shapename_state\tcomplete_groups_without_shapename_state\tcomplete_groups_with_primitive_type_state\tcomplete_groups_without_primitive_type_state\tcomplete_groups_with_unique_near_unit_float2\tcomplete_groups_with_slot1_near_unit_float2\tcomplete_groups_with_other_slot_near_unit_float2\tincomplete_five_slot_groups\trole_row_count_histogram\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var stateRows = ReadInt(row, "state_rows");
        var roleRows = ReadInt(row, "role_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var missingStateKeys = ReadInt(row, "missing_state_keys");
        var missingRoleKeys = ReadInt(row, "missing_role_keys");
        var duplicateStateKeys = ReadInt(row, "duplicate_state_keys");
        var duplicateRoleKeys = ReadInt(row, "duplicate_role_keys");
        var stateInputStatusValidRows = ReadInt(row, "state_input_status_valid_rows");
        var roleInputStatusValidRows = ReadInt(row, "role_input_status_valid_rows");
        var primitiveGroups = ReadInt(row, "primitive_groups");
        var groupsWithFiveRoleRows = ReadInt(row, "groups_with_five_role_rows");
        var completeFiveSlotGroups = ReadInt(row, "complete_five_slot_groups");
        var stateRelocationRows = ReadInt(row, "state_relocation_rows");
        var shapenameStateRows = ReadInt(row, "shapename_state_rows");
        var primitiveTypeStateRows = ReadInt(row, "primitive_type_state_rows");
        var exactAdjacencyStreamRows = ReadInt(row, "exact_adjacency_stream_rows");
        var completeGroupsWithShapenameState = ReadInt(row, "complete_groups_with_shapename_state");
        var completeGroupsWithoutShapenameState = ReadInt(row, "complete_groups_without_shapename_state");
        var completeGroupsWithPrimitiveTypeState = ReadInt(row, "complete_groups_with_primitive_type_state");
        var completeGroupsWithoutPrimitiveTypeState = ReadInt(row, "complete_groups_without_primitive_type_state");
        var completeGroupsWithUniqueNearUnitFloat2 = ReadInt(row, "complete_groups_with_unique_near_unit_float2");
        var completeGroupsWithSlot1NearUnitFloat2 = ReadInt(row, "complete_groups_with_slot1_near_unit_float2");
        var completeGroupsWithOtherSlotNearUnitFloat2 = ReadInt(row, "complete_groups_with_other_slot_near_unit_float2");
        var incompleteFiveSlotGroups = ReadInt(row, "incomplete_five_slot_groups");
        var roleRowCountHistogram = Get(row, "role_row_count_histogram");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-numeric-slot-cooccurrence-control", StringComparison.Ordinal) &&
                    stateRows > 0 &&
                    roleRows > 0 &&
                    joinedRows == stateRows &&
                    missingStateKeys == 0 &&
                    missingRoleKeys == 0 &&
                    duplicateStateKeys == 0 &&
                    duplicateRoleKeys == 0 &&
                    stateInputStatusValidRows == stateRows &&
                    roleInputStatusValidRows == roleRows &&
                    primitiveGroups > 0 &&
                    groupsWithFiveRoleRows == primitiveGroups &&
                    completeFiveSlotGroups > 0 &&
                    stateRelocationRows == stateRows &&
                    shapenameStateRows >= 0 &&
                    primitiveTypeStateRows >= 0 &&
                    exactAdjacencyStreamRows == stateRows &&
                    completeGroupsWithUniqueNearUnitFloat2 == completeFiveSlotGroups &&
                    completeGroupsWithSlot1NearUnitFloat2 == completeFiveSlotGroups &&
                    completeGroupsWithOtherSlotNearUnitFloat2 == 0 &&
                    incompleteFiveSlotGroups >= 0 &&
                    !string.IsNullOrWhiteSpace(roleRowCountHistogram) &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            stateRows,
            roleRows,
            joinedRows,
            missingStateKeys,
            missingRoleKeys,
            duplicateStateKeys,
            duplicateRoleKeys,
            stateInputStatusValidRows,
            roleInputStatusValidRows,
            primitiveGroups,
            groupsWithFiveRoleRows,
            completeFiveSlotGroups,
            stateRelocationRows,
            shapenameStateRows,
            primitiveTypeStateRows,
            exactAdjacencyStreamRows,
            completeGroupsWithShapenameState,
            completeGroupsWithoutShapenameState,
            completeGroupsWithPrimitiveTypeState,
            completeGroupsWithoutPrimitiveTypeState,
            completeGroupsWithUniqueNearUnitFloat2,
            completeGroupsWithSlot1NearUnitFloat2,
            completeGroupsWithOtherSlotNearUnitFloat2,
            incompleteFiveSlotGroups,
            roleRowCountHistogram,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalCommandSlotRoleCooccurrenceEvidence ReadOriginalCommandSlotRoleCooccurrenceEvidence(string path)
    {
        const string expectedHeader =
            "descriptor_rows\trole_rows\tjoined_rows\tmissing_descriptor_keys\tmissing_role_keys\tduplicate_descriptor_keys\tduplicate_role_keys\tdescriptor_input_status_valid_rows\trole_input_status_valid_rows\tcurrent_ordinal_present_rows\tcurrent_ordinal_identity_match_rows\tcurrent_ordinal_uniquely_identified_rows\tshifted_ordinal_identity_match_rows\tprimitive_groups\tgroups_with_five_role_rows\tcomplete_five_slot_groups\tcomplete_groups_with_unique_near_unit_float2\tcomplete_groups_with_slot1_near_unit_float2\tcomplete_groups_with_other_slot_near_unit_float2\tincomplete_five_slot_groups\trole_row_count_histogram\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var descriptorRows = ReadInt(row, "descriptor_rows");
        var roleRows = ReadInt(row, "role_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var missingDescriptorKeys = ReadInt(row, "missing_descriptor_keys");
        var missingRoleKeys = ReadInt(row, "missing_role_keys");
        var duplicateDescriptorKeys = ReadInt(row, "duplicate_descriptor_keys");
        var duplicateRoleKeys = ReadInt(row, "duplicate_role_keys");
        var descriptorInputStatusValidRows = ReadInt(row, "descriptor_input_status_valid_rows");
        var roleInputStatusValidRows = ReadInt(row, "role_input_status_valid_rows");
        var currentOrdinalPresentRows = ReadInt(row, "current_ordinal_present_rows");
        var currentOrdinalIdentityMatchRows = ReadInt(row, "current_ordinal_identity_match_rows");
        var currentOrdinalUniquelyIdentifiedRows = ReadInt(row, "current_ordinal_uniquely_identified_rows");
        var shiftedOrdinalIdentityMatchRows = ReadInt(row, "shifted_ordinal_identity_match_rows");
        var primitiveGroups = ReadInt(row, "primitive_groups");
        var groupsWithFiveRoleRows = ReadInt(row, "groups_with_five_role_rows");
        var completeFiveSlotGroups = ReadInt(row, "complete_five_slot_groups");
        var completeGroupsWithUniqueNearUnitFloat2 = ReadInt(row, "complete_groups_with_unique_near_unit_float2");
        var completeGroupsWithSlot1NearUnitFloat2 = ReadInt(row, "complete_groups_with_slot1_near_unit_float2");
        var completeGroupsWithOtherSlotNearUnitFloat2 = ReadInt(row, "complete_groups_with_other_slot_near_unit_float2");
        var incompleteFiveSlotGroups = ReadInt(row, "incomplete_five_slot_groups");
        var roleRowCountHistogram = Get(row, "role_row_count_histogram");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-command-slot-role-cooccurrence-control", StringComparison.Ordinal) &&
                    descriptorRows > 0 &&
                    roleRows > 0 &&
                    joinedRows == descriptorRows &&
                    missingDescriptorKeys == 0 &&
                    missingRoleKeys == 0 &&
                    duplicateDescriptorKeys == 0 &&
                    duplicateRoleKeys == 0 &&
                    descriptorInputStatusValidRows == descriptorRows &&
                    roleInputStatusValidRows == roleRows &&
                    currentOrdinalPresentRows == descriptorRows &&
                    currentOrdinalIdentityMatchRows == descriptorRows &&
                    currentOrdinalUniquelyIdentifiedRows == descriptorRows &&
                    shiftedOrdinalIdentityMatchRows == 0 &&
                    primitiveGroups > 0 &&
                    groupsWithFiveRoleRows == primitiveGroups &&
                    completeFiveSlotGroups > 0 &&
                    completeGroupsWithUniqueNearUnitFloat2 == completeFiveSlotGroups &&
                    completeGroupsWithSlot1NearUnitFloat2 == completeFiveSlotGroups &&
                    completeGroupsWithOtherSlotNearUnitFloat2 == 0 &&
                    incompleteFiveSlotGroups >= 0 &&
                    !string.IsNullOrWhiteSpace(roleRowCountHistogram) &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            descriptorRows,
            roleRows,
            joinedRows,
            missingDescriptorKeys,
            missingRoleKeys,
            duplicateDescriptorKeys,
            duplicateRoleKeys,
            descriptorInputStatusValidRows,
            roleInputStatusValidRows,
            currentOrdinalPresentRows,
            currentOrdinalIdentityMatchRows,
            currentOrdinalUniquelyIdentifiedRows,
            shiftedOrdinalIdentityMatchRows,
            primitiveGroups,
            groupsWithFiveRoleRows,
            completeFiveSlotGroups,
            completeGroupsWithUniqueNearUnitFloat2,
            completeGroupsWithSlot1NearUnitFloat2,
            completeGroupsWithOtherSlotNearUnitFloat2,
            incompleteFiveSlotGroups,
            roleRowCountHistogram,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalRenderStateGeometryBoundsControlEvidence ReadOriginalRenderStateGeometryBoundsControlEvidence(string path)
    {
        const string expectedHeader =
            "partition_object_rows\tbounds_object_rows\tjoined_object_rows\tpartition_input_status_valid_rows\tbounds_input_status_valid_rows\tobject_cardinality_match_rows\tbounds_group_rows\tcomplete_fetch_group_rows\texpected_incomplete_fetch_group_rows\tbounds_slot0_match_rows\tbounds_nonzero_match_rows\texact_command_shape_rows\tcomplete_control_rows\tsemantic_binding\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var partitionObjectRows = ReadInt(row, "partition_object_rows");
        var boundsObjectRows = ReadInt(row, "bounds_object_rows");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var partitionInputStatusValidRows = ReadInt(row, "partition_input_status_valid_rows");
        var boundsInputStatusValidRows = ReadInt(row, "bounds_input_status_valid_rows");
        var objectCardinalityMatchRows = ReadInt(row, "object_cardinality_match_rows");
        var boundsGroupRows = ReadInt(row, "bounds_group_rows");
        var completeFetchGroupRows = ReadInt(row, "complete_fetch_group_rows");
        var expectedIncompleteFetchGroupRows = ReadInt(row, "expected_incomplete_fetch_group_rows");
        var boundsSlot0MatchRows = ReadInt(row, "bounds_slot0_match_rows");
        var boundsNonzeroMatchRows = ReadInt(row, "bounds_nonzero_match_rows");
        var exactCommandShapeRows = ReadInt(row, "exact_command_shape_rows");
        var completeControlRows = ReadInt(row, "complete_control_rows");
        var semanticBinding = Get(row, "semantic_binding");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-render-state-geometry-bounds-structural-control", StringComparison.Ordinal) &&
                    partitionObjectRows == 24 &&
                    boundsObjectRows == 24 &&
                    joinedObjectRows == 24 &&
                    partitionInputStatusValidRows == 24 &&
                    boundsInputStatusValidRows == 24 &&
                    objectCardinalityMatchRows == 24 &&
                    boundsGroupRows == 218 &&
                    completeFetchGroupRows == 216 &&
                    expectedIncompleteFetchGroupRows == 2 &&
                    boundsSlot0MatchRows == 218 &&
                    boundsNonzeroMatchRows == 0 &&
                    exactCommandShapeRows == 0 &&
                    completeControlRows == 24 &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            partitionObjectRows,
            boundsObjectRows,
            joinedObjectRows,
            partitionInputStatusValidRows,
            boundsInputStatusValidRows,
            objectCardinalityMatchRows,
            boundsGroupRows,
            completeFetchGroupRows,
            expectedIncompleteFetchGroupRows,
            boundsSlot0MatchRows,
            boundsNonzeroMatchRows,
            exactCommandShapeRows,
            completeControlRows,
            semanticBinding,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalStatePopulationControlEvidence ReadOriginalStatePopulationControlEvidence(string path)
    {
        const string expectedHeader =
            "source_report\tobject_rows\tprimitive_rows\tshapename_rows\tprimitive_type_rows\tpopulation_overlap_rows\tsame_primitive_index_pairs\tadjacent_primitive_pairs\tsame_layer_pairs\texact_command_hash_pairs\texact_boundary_shape_pairs\tcomplete_shapename_rows\tcomplete_primitive_type_rows\tobjects_with_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var objectRows = ReadInt(row, "object_rows");
        var primitiveRows = ReadInt(row, "primitive_rows");
        var shapeNameRows = ReadInt(row, "shapename_rows");
        var primitiveTypeRows = ReadInt(row, "primitive_type_rows");
        var populationOverlapRows = ReadInt(row, "population_overlap_rows");
        var samePrimitiveIndexPairs = ReadInt(row, "same_primitive_index_pairs");
        var adjacentPrimitivePairs = ReadInt(row, "adjacent_primitive_pairs");
        var sameLayerPairs = ReadInt(row, "same_layer_pairs");
        var exactCommandHashPairs = ReadInt(row, "exact_command_hash_pairs");
        var exactBoundaryShapePairs = ReadInt(row, "exact_boundary_shape_pairs");
        var completeShapeNameRows = ReadInt(row, "complete_shapename_rows");
        var completePrimitiveTypeRows = ReadInt(row, "complete_primitive_type_rows");
        var objectsWithRows = ReadInt(row, "objects_with_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-bounded-original-state-population-control", StringComparison.Ordinal) &&
                    objectRows > 0 &&
                    primitiveRows > 0 &&
                    shapeNameRows >= 0 &&
                    primitiveTypeRows >= 0 &&
                    populationOverlapRows == 0 &&
                    samePrimitiveIndexPairs >= 0 &&
                    adjacentPrimitivePairs >= 0 &&
                    sameLayerPairs >= 0 &&
                    exactCommandHashPairs >= 0 &&
                    exactBoundaryShapePairs >= 0 &&
                    completeShapeNameRows >= 0 &&
                    completePrimitiveTypeRows >= 0 &&
                    objectsWithRows == objectRows &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            objectRows,
            primitiveRows,
            shapeNameRows,
            primitiveTypeRows,
            populationOverlapRows,
            samePrimitiveIndexPairs,
            adjacentPrimitivePairs,
            sameLayerPairs,
            exactCommandHashPairs,
            exactBoundaryShapePairs,
            completeShapeNameRows,
            completePrimitiveTypeRows,
            objectsWithRows,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalStateTokenEvidence ReadOriginalStateTokenEvidence(string path)
    {
        const string expectedHeader =
            "model_archive_path\tinput_rows\tstate_rows\tvalid_input_rows\tshapename_rows\tclamp_u_rows\tclamp_v_rows\tprimitive_type_rows\ttexture_token_rows\tmaterial_token_rows\tuv_coordinate_token_rows\tvertex_attribute_token_rows\tdeformation_token_rows\tassignment_name_catalog\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var inputRows = ReadInt(row, "input_rows");
        var stateRows = ReadInt(row, "state_rows");
        var validInputRows = ReadInt(row, "valid_input_rows");
        var shapeNameRows = ReadInt(row, "shapename_rows");
        var clampURows = ReadInt(row, "clamp_u_rows");
        var clampVRows = ReadInt(row, "clamp_v_rows");
        var primitiveTypeRows = ReadInt(row, "primitive_type_rows");
        var textureTokenRows = ReadInt(row, "texture_token_rows");
        var materialTokenRows = ReadInt(row, "material_token_rows");
        var uvCoordinateTokenRows = ReadInt(row, "uv_coordinate_token_rows");
        var vertexAttributeTokenRows = ReadInt(row, "vertex_attribute_token_rows");
        var deformationTokenRows = ReadInt(row, "deformation_token_rows");
        var assignmentNameCatalog = Get(row, "assignment_name_catalog");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, EaglOriginalStateTokenAudit.Status, StringComparison.Ordinal) &&
                    inputRows > 0 &&
                    stateRows == inputRows &&
                    validInputRows == inputRows &&
                    shapeNameRows >= 0 &&
                    clampURows == shapeNameRows &&
                    clampVRows == shapeNameRows &&
                    primitiveTypeRows >= 0 &&
                    textureTokenRows == 0 &&
                    materialTokenRows == 0 &&
                    uvCoordinateTokenRows == 0 &&
                    vertexAttributeTokenRows == 0 &&
                    deformationTokenRows == 0 &&
                    !string.IsNullOrWhiteSpace(assignmentNameCatalog) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            inputRows,
            stateRows,
            validInputRows,
            shapeNameRows,
            clampURows,
            clampVRows,
            primitiveTypeRows,
            textureTokenRows,
            materialTokenRows,
            uvCoordinateTokenRows,
            vertexAttributeTokenRows,
            deformationTokenRows,
            assignmentNameCatalog,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalStateCandidateAttributeControlEvidence
        ReadOriginalStateCandidateAttributeControlEvidence(string path)
    {
        const string expectedHeader =
            "state_source_report\tattribute_source_report\tinfluence_source_report\tcandidate_object_rows\tstate_object_rows\tinfluence_object_rows\tjoined_object_rows\tmissing_state_objects\tmissing_attribute_objects\tmissing_influence_objects\tstate_rows_joined\tstate_valid_rows\tstate_negative_token_rows\tattribute_complete_rows\tattribute_finite_rows\tattribute_near_unit_rows\tinfluence_geometry_rows\tinfluence_status_rows\tinfluence_texture_rows\tinfluence_mesh_shape_state_exact_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var candidateObjectRows = ReadInt(row, "candidate_object_rows");
        var stateObjectRows = ReadInt(row, "state_object_rows");
        var influenceObjectRows = ReadInt(row, "influence_object_rows");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var missingStateObjects = ReadInt(row, "missing_state_objects");
        var missingAttributeObjects = ReadInt(row, "missing_attribute_objects");
        var missingInfluenceObjects = ReadInt(row, "missing_influence_objects");
        var stateRowsJoined = ReadInt(row, "state_rows_joined");
        var stateValidRows = ReadInt(row, "state_valid_rows");
        var stateNegativeTokenRows = ReadInt(row, "state_negative_token_rows");
        var attributeCompleteRows = ReadInt(row, "attribute_complete_rows");
        var attributeFiniteRows = ReadInt(row, "attribute_finite_rows");
        var attributeNearUnitRows = ReadInt(row, "attribute_near_unit_rows");
        var influenceGeometryRows = ReadInt(row, "influence_geometry_rows");
        var influenceStatusRows = ReadInt(row, "influence_status_rows");
        var influenceTextureRows = ReadInt(row, "influence_texture_rows");
        var influenceMeshShapeStateExactRows = ReadInt(row, "influence_mesh_shape_state_exact_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-candidate-attribute-control", StringComparison.Ordinal) &&
                    candidateObjectRows == 24 &&
                    stateObjectRows >= candidateObjectRows &&
                    influenceObjectRows == candidateObjectRows &&
                    joinedObjectRows == candidateObjectRows &&
                    missingStateObjects == 0 &&
                    missingAttributeObjects == 0 &&
                    missingInfluenceObjects == 0 &&
                    stateRowsJoined > 0 &&
                    stateValidRows == stateRowsJoined &&
                    stateNegativeTokenRows == stateRowsJoined &&
                    attributeCompleteRows == candidateObjectRows &&
                    attributeFiniteRows == candidateObjectRows &&
                    attributeNearUnitRows == candidateObjectRows &&
                    influenceGeometryRows == candidateObjectRows &&
                    influenceStatusRows == candidateObjectRows &&
                    influenceMeshShapeStateExactRows == 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            candidateObjectRows,
            stateObjectRows,
            influenceObjectRows,
            joinedObjectRows,
            missingStateObjects,
            missingAttributeObjects,
            missingInfluenceObjects,
            stateRowsJoined,
            stateValidRows,
            stateNegativeTokenRows,
            attributeCompleteRows,
            attributeFiniteRows,
            attributeNearUnitRows,
            influenceGeometryRows,
            influenceStatusRows,
            influenceTextureRows,
            influenceMeshShapeStateExactRows,
            status,
            provenance,
            originalSemantics);
    }

    private static CandidateAttributeTextureCompatibilityEvidence
        ReadCandidateAttributeTextureCompatibilityEvidence(string path)
    {
        const string expectedHeader =
            "attribute_object_rows\tasset_object_rows\tjoined_object_rows\tattribute_input_status_valid_rows\tasset_input_status_valid_rows\ttexture_dimensions_valid_rows\ttexture_pal8_rows\tdecoded_pixel_match_rows\tslot1_range_rows\tslot1_finite_range_rows\tslot1_normalized_range_rows\tslot1_value_count_match_vertex_rows\tattribute_texture_compatibility_rows\tsemantic_binding\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var attributeObjectRows = ReadInt(row, "attribute_object_rows");
        var assetObjectRows = ReadInt(row, "asset_object_rows");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var attributeInputStatusValidRows = ReadInt(row, "attribute_input_status_valid_rows");
        var assetInputStatusValidRows = ReadInt(row, "asset_input_status_valid_rows");
        var textureDimensionsValidRows = ReadInt(row, "texture_dimensions_valid_rows");
        var texturePal8Rows = ReadInt(row, "texture_pal8_rows");
        var decodedPixelMatchRows = ReadInt(row, "decoded_pixel_match_rows");
        var slot1RangeRows = ReadInt(row, "slot1_range_rows");
        var slot1FiniteRangeRows = ReadInt(row, "slot1_finite_range_rows");
        var slot1NormalizedRangeRows = ReadInt(row, "slot1_normalized_range_rows");
        var slot1ValueCountMatchVertexRows = ReadInt(row, "slot1_value_count_match_vertex_rows");
        var attributeTextureCompatibilityRows = ReadInt(row, "attribute_texture_compatibility_rows");
        var semanticBinding = Get(row, "semantic_binding");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-candidate-attribute-texture-compatibility-control", StringComparison.Ordinal) &&
                    attributeObjectRows == 24 &&
                    assetObjectRows == 24 &&
                    joinedObjectRows == 24 &&
                    attributeInputStatusValidRows == 24 &&
                    assetInputStatusValidRows == 24 &&
                    textureDimensionsValidRows == 24 &&
                    texturePal8Rows == 24 &&
                    decodedPixelMatchRows == 24 &&
                    slot1RangeRows == 24 &&
                    slot1FiniteRangeRows == 24 &&
                    slot1NormalizedRangeRows == 24 &&
                    slot1ValueCountMatchVertexRows == 24 &&
                    attributeTextureCompatibilityRows == 24 &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            attributeObjectRows,
            assetObjectRows,
            joinedObjectRows,
            attributeInputStatusValidRows,
            assetInputStatusValidRows,
            textureDimensionsValidRows,
            texturePal8Rows,
            decodedPixelMatchRows,
            slot1RangeRows,
            slot1FiniteRangeRows,
            slot1NormalizedRangeRows,
            slot1ValueCountMatchVertexRows,
            attributeTextureCompatibilityRows,
            semanticBinding,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalShapenameCandidateAssetControlEvidence
        ReadOriginalShapenameCandidateAssetControlEvidence(string path)
    {
        const string expectedHeader =
            "state_source_report\tcandidate_source_report\tstate_rows\tcandidate_rows\tjoined_rows\tmissing_candidate_objects\tmissing_state_objects\tstate_input_valid_rows\tcandidate_input_valid_rows\tobject_state_exact_rows\tshapename_token_match_rows\tsame_trunk_rows\tsource_chain_valid_rows\tshape_entry_exact_rows\tdecoded_pixel_match_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var stateRows = ReadInt(row, "state_rows");
        var candidateRows = ReadInt(row, "candidate_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var missingCandidateObjects = ReadInt(row, "missing_candidate_objects");
        var missingStateObjects = ReadInt(row, "missing_state_objects");
        var stateInputValidRows = ReadInt(row, "state_input_valid_rows");
        var candidateInputValidRows = ReadInt(row, "candidate_input_valid_rows");
        var objectStateExactRows = ReadInt(row, "object_state_exact_rows");
        var shapenameTokenMatchRows = ReadInt(row, "shapename_token_match_rows");
        var sameTrunkRows = ReadInt(row, "same_trunk_rows");
        var sourceChainValidRows = ReadInt(row, "source_chain_valid_rows");
        var shapeEntryExactRows = ReadInt(row, "shape_entry_exact_rows");
        var decodedPixelMatchRows = ReadInt(row, "decoded_pixel_match_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-shapename-candidate-asset-control", StringComparison.Ordinal) &&
                    stateRows == 24 &&
                    candidateRows == 24 &&
                    joinedRows == 24 &&
                    missingCandidateObjects == 0 &&
                    missingStateObjects == 0 &&
                    stateInputValidRows == 24 &&
                    candidateInputValidRows == 24 &&
                    objectStateExactRows == 24 &&
                    shapenameTokenMatchRows == 24 &&
                    sameTrunkRows == 24 &&
                    sourceChainValidRows == 24 &&
                    shapeEntryExactRows == 24 &&
                    decodedPixelMatchRows == 24 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            stateRows,
            candidateRows,
            joinedRows,
            missingCandidateObjects,
            missingStateObjects,
            stateInputValidRows,
            candidateInputValidRows,
            objectStateExactRows,
            shapenameTokenMatchRows,
            sameTrunkRows,
            sourceChainValidRows,
            shapeEntryExactRows,
            decodedPixelMatchRows,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalStateCandidatePrimitiveShapeControlEvidence
        ReadOriginalStateCandidatePrimitiveShapeControlEvidence(string path)
    {
        const string expectedHeader =
            "state_source_report\tcandidate_source_report\tstate_rows\tcandidate_rows\tjoined_rows\tmissing_candidate_objects\tmissing_state_objects\tstate_input_valid_rows\tcandidate_input_valid_rows\tstate_primitive_count_match_rows\tshapename_count_match_rows\tcandidate_shape_state_streams_complete_rows\tsame_trunk_rows\tsource_chain_valid_rows\tshape_entry_exact_rows\tdecoded_pixel_match_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var stateRows = ReadInt(row, "state_rows");
        var candidateRows = ReadInt(row, "candidate_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var missingCandidateObjects = ReadInt(row, "missing_candidate_objects");
        var missingStateObjects = ReadInt(row, "missing_state_objects");
        var stateInputValidRows = ReadInt(row, "state_input_valid_rows");
        var candidateInputValidRows = ReadInt(row, "candidate_input_valid_rows");
        var statePrimitiveCountMatchRows = ReadInt(row, "state_primitive_count_match_rows");
        var shapenameCountMatchRows = ReadInt(row, "shapename_count_match_rows");
        var candidateShapeStateStreamsCompleteRows = ReadInt(row, "candidate_shape_state_streams_complete_rows");
        var sameTrunkRows = ReadInt(row, "same_trunk_rows");
        var sourceChainValidRows = ReadInt(row, "source_chain_valid_rows");
        var shapeEntryExactRows = ReadInt(row, "shape_entry_exact_rows");
        var decodedPixelMatchRows = ReadInt(row, "decoded_pixel_match_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-candidate-primitive-shape-control", StringComparison.Ordinal) &&
                    stateRows == 24 &&
                    candidateRows == 24 &&
                    joinedRows == 24 &&
                    missingCandidateObjects == 0 &&
                    missingStateObjects == 0 &&
                    stateInputValidRows == 24 &&
                    candidateInputValidRows == 24 &&
                    statePrimitiveCountMatchRows == 24 &&
                    shapenameCountMatchRows == 24 &&
                    candidateShapeStateStreamsCompleteRows == 24 &&
                    sameTrunkRows == 24 &&
                    sourceChainValidRows == 24 &&
                    shapeEntryExactRows == 24 &&
                    decodedPixelMatchRows == 24 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            stateRows,
            candidateRows,
            joinedRows,
            missingCandidateObjects,
            missingStateObjects,
            stateInputValidRows,
            candidateInputValidRows,
            statePrimitiveCountMatchRows,
            shapenameCountMatchRows,
            candidateShapeStateStreamsCompleteRows,
            sameTrunkRows,
            sourceChainValidRows,
            shapeEntryExactRows,
            decodedPixelMatchRows,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalCandidateShapenamePrimitiveIndexControlEvidence
        ReadOriginalCandidateShapenamePrimitiveIndexControlEvidence(string path)
    {
        const string expectedHeader =
            "state_source_report\tcandidate_source_report\tstate_rows\tcandidate_rows\tjoined_rows\tmissing_candidate_objects\tmissing_state_objects\tsequence_match_rows\tset_match_rows\tcount_match_rows\tcomplete_candidate_rows\toriginal_input_valid_rows\tcandidate_input_valid_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var stateRows = ReadInt(row, "state_rows");
        var candidateRows = ReadInt(row, "candidate_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var missingCandidateObjects = ReadInt(row, "missing_candidate_objects");
        var missingStateObjects = ReadInt(row, "missing_state_objects");
        var sequenceMatchRows = ReadInt(row, "sequence_match_rows");
        var setMatchRows = ReadInt(row, "set_match_rows");
        var countMatchRows = ReadInt(row, "count_match_rows");
        var completeCandidateRows = ReadInt(row, "complete_candidate_rows");
        var originalInputValidRows = ReadInt(row, "original_input_valid_rows");
        var candidateInputValidRows = ReadInt(row, "candidate_input_valid_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-candidate-shapename-primitive-index-control", StringComparison.Ordinal) &&
                    stateRows == 24 &&
                    candidateRows == 24 &&
                    joinedRows == 24 &&
                    missingCandidateObjects == 0 &&
                    missingStateObjects == 0 &&
                    sequenceMatchRows == 24 &&
                    setMatchRows == 24 &&
                    countMatchRows == 24 &&
                    completeCandidateRows == 24 &&
                    originalInputValidRows == 24 &&
                    candidateInputValidRows == 24 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            stateRows,
            candidateRows,
            joinedRows,
            missingCandidateObjects,
            missingStateObjects,
            sequenceMatchRows,
            setMatchRows,
            countMatchRows,
            completeCandidateRows,
            originalInputValidRows,
            candidateInputValidRows,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalCandidatePrimitiveFetchControlEvidence
        ReadOriginalCandidatePrimitiveFetchControlEvidence(string path)
    {
        const string expectedHeader =
            "original_source_report\tcandidate_source_report\toriginal_rows\tcandidate_rows\tjoined_rows\tmissing_candidate_keys\tmissing_original_keys\tcommand_count_match_rows\tcommand_bytes_sha256_match_rows\tcommand_stream_complete_match_rows\tfetch_slot_count_match_rows\tfetch_slot_id_sequence_match_rows\tcomplete_five_slot_sequence_match_rows\tstate_assignment_sequence_match_rows\tprimitive_type_flag_match_rows\tcandidate_slot_records_valid_rows\tcandidate_slot_records_unmaterialized_expected_rows\toriginal_input_valid_rows\tcandidate_input_valid_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var originalRows = ReadInt(row, "original_rows");
        var candidateRows = ReadInt(row, "candidate_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var missingCandidateKeys = ReadInt(row, "missing_candidate_keys");
        var missingOriginalKeys = ReadInt(row, "missing_original_keys");
        var commandCountMatchRows = ReadInt(row, "command_count_match_rows");
        var commandBytesSha256MatchRows = ReadInt(row, "command_bytes_sha256_match_rows");
        var commandStreamCompleteMatchRows = ReadInt(row, "command_stream_complete_match_rows");
        var fetchSlotCountMatchRows = ReadInt(row, "fetch_slot_count_match_rows");
        var fetchSlotIdSequenceMatchRows = ReadInt(row, "fetch_slot_id_sequence_match_rows");
        var completeFiveSlotSequenceMatchRows = ReadInt(row, "complete_five_slot_sequence_match_rows");
        var stateAssignmentSequenceMatchRows = ReadInt(row, "state_assignment_sequence_match_rows");
        var primitiveTypeFlagMatchRows = ReadInt(row, "primitive_type_flag_match_rows");
        var candidateSlotRecordsValidRows = ReadInt(row, "candidate_slot_records_valid_rows");
        var candidateSlotRecordsUnmaterializedExpectedRows = ReadInt(row, "candidate_slot_records_unmaterialized_expected_rows");
        var originalInputValidRows = ReadInt(row, "original_input_valid_rows");
        var candidateInputValidRows = ReadInt(row, "candidate_input_valid_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-candidate-primitive-fetch-control", StringComparison.Ordinal) &&
                    originalRows == 2364 &&
                    candidateRows == 218 &&
                    joinedRows == 218 &&
                    missingCandidateKeys == 2146 &&
                     missingOriginalKeys == 0 &&
                     commandCountMatchRows == 218 &&
                     commandBytesSha256MatchRows == 218 &&
                     commandStreamCompleteMatchRows == 218 &&
                    fetchSlotCountMatchRows == 218 &&
                    fetchSlotIdSequenceMatchRows == 216 &&
                    completeFiveSlotSequenceMatchRows == 218 &&
                    stateAssignmentSequenceMatchRows == 218 &&
                    primitiveTypeFlagMatchRows == 218 &&
                    candidateSlotRecordsValidRows == 216 &&
                    candidateSlotRecordsUnmaterializedExpectedRows == 2 &&
                    originalInputValidRows == 218 &&
                    candidateInputValidRows == 218 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            originalRows,
            candidateRows,
            joinedRows,
            missingCandidateKeys,
            missingOriginalKeys,
            commandCountMatchRows,
            commandBytesSha256MatchRows,
            commandStreamCompleteMatchRows,
            fetchSlotCountMatchRows,
            fetchSlotIdSequenceMatchRows,
            completeFiveSlotSequenceMatchRows,
            stateAssignmentSequenceMatchRows,
            primitiveTypeFlagMatchRows,
            candidateSlotRecordsValidRows,
            candidateSlotRecordsUnmaterializedExpectedRows,
            originalInputValidRows,
            candidateInputValidRows,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalGeometryDrawClassificationEvidence
        ReadOriginalGeometryDrawClassificationEvidence(string path)
    {
        const string expectedHeader =
            "original_source_report\tcandidate_source_report\tcandidate_object_rows\toriginal_scoped_rows\tcandidate_rows\tjoined_rows\tmissing_original_keys\tmissing_candidate_keys\tjoined_object_rows\tobjects_with_complete_classification\toriginal_input_status_valid_rows\tcandidate_input_status_valid_rows\toriginal_geometry_marker_rows\toriginal_state_marker_rows\tcandidate_geometry_marker_rows\tcandidate_state_marker_rows\tclassification_match_rows\tstate_count_greater_than_geometry_rows\tstate_to_geometry_unique_binding_proven_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var candidateObjectRows = ReadInt(row, "candidate_object_rows");
        var originalScopedRows = ReadInt(row, "original_scoped_rows");
        var candidateRows = ReadInt(row, "candidate_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var missingOriginalKeys = ReadInt(row, "missing_original_keys");
        var missingCandidateKeys = ReadInt(row, "missing_candidate_keys");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var objectsWithCompleteClassification = ReadInt(row, "objects_with_complete_classification");
        var originalInputStatusValidRows = ReadInt(row, "original_input_status_valid_rows");
        var candidateInputStatusValidRows = ReadInt(row, "candidate_input_status_valid_rows");
        var originalGeometryMarkerRows = ReadInt(row, "original_geometry_marker_rows");
        var originalStateMarkerRows = ReadInt(row, "original_state_marker_rows");
        var candidateGeometryMarkerRows = ReadInt(row, "candidate_geometry_marker_rows");
        var candidateStateMarkerRows = ReadInt(row, "candidate_state_marker_rows");
        var classificationMatchRows = ReadInt(row, "classification_match_rows");
        var stateCountGreaterThanGeometryRows = ReadInt(row, "state_count_greater_than_geometry_rows");
        var stateToGeometryUniqueBindingProvenRows = ReadInt(row, "state_to_geometry_unique_binding_proven_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-geometry-draw-classification-control", StringComparison.Ordinal) &&
                    candidateObjectRows == 24 &&
                    originalScopedRows == 218 &&
                    candidateRows == 218 &&
                    joinedRows == 218 &&
                    missingOriginalKeys == 0 &&
                    missingCandidateKeys == 0 &&
                    joinedObjectRows == 24 &&
                    objectsWithCompleteClassification == 24 &&
                    originalInputStatusValidRows == 218 &&
                    candidateInputStatusValidRows == 218 &&
                    originalGeometryMarkerRows == 38 &&
                    originalStateMarkerRows == 180 &&
                    candidateGeometryMarkerRows == 38 &&
                    candidateStateMarkerRows == 180 &&
                    classificationMatchRows == 218 &&
                    stateCountGreaterThanGeometryRows == 24 &&
                    stateToGeometryUniqueBindingProvenRows == 0 &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            candidateObjectRows,
            originalScopedRows,
            candidateRows,
            joinedRows,
            missingOriginalKeys,
            missingCandidateKeys,
            joinedObjectRows,
            objectsWithCompleteClassification,
            originalInputStatusValidRows,
            candidateInputStatusValidRows,
            originalGeometryMarkerRows,
            originalStateMarkerRows,
            candidateGeometryMarkerRows,
            candidateStateMarkerRows,
            classificationMatchRows,
            stateCountGreaterThanGeometryRows,
            stateToGeometryUniqueBindingProvenRows,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalStateCandidateMaterialAmbiguityEvidence
        ReadOriginalStateCandidateMaterialAmbiguityEvidence(string path)
    {
        const string expectedHeader =
            "render_source_report\torder_source_report\tstate_asset_source_report\tcandidate_asset_source_report\tcandidate_object_rows\trender_scoped_rows\torder_rows\torder_state_rows\toriginal_geometry_rows\toriginal_state_rows\tnonempty_state_interval_rows\tintervals_with_two_adjacent_geometry_candidates\tintervals_with_one_adjacent_geometry_candidate\tintervals_with_zero_adjacent_geometry_candidates\tstate_rows_joined\tstate_rows_with_shape_token_match\tstate_rows_with_candidate_asset_source\tcandidate_asset_valid_rows\tobject_asset_join_rows\tobjects_with_complete_ambiguity_control\toriginal_input_status_valid_rows\torder_input_status_valid_rows\tstate_asset_input_status_valid_rows\tcandidate_asset_input_status_valid_rows\tstate_to_geometry_unique_binding_proven_rows\tstate_to_material_unique_binding_proven_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var candidateObjectRows = ReadInt(row, "candidate_object_rows");
        var renderScopedRows = ReadInt(row, "render_scoped_rows");
        var orderRows = ReadInt(row, "order_rows");
        var orderStateRows = ReadInt(row, "order_state_rows");
        var originalGeometryRows = ReadInt(row, "original_geometry_rows");
        var originalStateRows = ReadInt(row, "original_state_rows");
        var nonemptyStateIntervalRows = ReadInt(row, "nonempty_state_interval_rows");
        var intervalsWithTwoAdjacentGeometryCandidates = ReadInt(row, "intervals_with_two_adjacent_geometry_candidates");
        var intervalsWithOneAdjacentGeometryCandidate = ReadInt(row, "intervals_with_one_adjacent_geometry_candidate");
        var intervalsWithZeroAdjacentGeometryCandidates = ReadInt(row, "intervals_with_zero_adjacent_geometry_candidates");
        var stateRowsJoined = ReadInt(row, "state_rows_joined");
        var stateRowsWithShapeTokenMatch = ReadInt(row, "state_rows_with_shape_token_match");
        var stateRowsWithCandidateAssetSource = ReadInt(row, "state_rows_with_candidate_asset_source");
        var candidateAssetValidRows = ReadInt(row, "candidate_asset_valid_rows");
        var objectAssetJoinRows = ReadInt(row, "object_asset_join_rows");
        var objectsWithCompleteAmbiguityControl = ReadInt(row, "objects_with_complete_ambiguity_control");
        var originalInputStatusValidRows = ReadInt(row, "original_input_status_valid_rows");
        var orderInputStatusValidRows = ReadInt(row, "order_input_status_valid_rows");
        var stateAssetInputStatusValidRows = ReadInt(row, "state_asset_input_status_valid_rows");
        var candidateAssetInputStatusValidRows = ReadInt(row, "candidate_asset_input_status_valid_rows");
        var stateToGeometryUniqueBindingProvenRows = ReadInt(row, "state_to_geometry_unique_binding_proven_rows");
        var stateToMaterialUniqueBindingProvenRows = ReadInt(row, "state_to_material_unique_binding_proven_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-candidate-material-ambiguity-control", StringComparison.Ordinal) &&
                    candidateObjectRows == 24 &&
                    renderScopedRows == 218 &&
                    orderRows == 62 &&
                    orderStateRows == 180 &&
                    originalGeometryRows == 38 &&
                    originalStateRows == 180 &&
                    nonemptyStateIntervalRows == 36 &&
                    intervalsWithTwoAdjacentGeometryCandidates == 12 &&
                    intervalsWithOneAdjacentGeometryCandidate == 24 &&
                    intervalsWithZeroAdjacentGeometryCandidates == 0 &&
                    stateRowsJoined == 180 &&
                    stateRowsWithShapeTokenMatch == 180 &&
                    stateRowsWithCandidateAssetSource == 180 &&
                    candidateAssetValidRows == 24 &&
                    objectAssetJoinRows == 24 &&
                    objectsWithCompleteAmbiguityControl == 24 &&
                    originalInputStatusValidRows == 218 &&
                    orderInputStatusValidRows == 62 &&
                    stateAssetInputStatusValidRows == 24 &&
                    candidateAssetInputStatusValidRows == 24 &&
                    stateToGeometryUniqueBindingProvenRows == 0 &&
                    stateToMaterialUniqueBindingProvenRows == 0 &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            candidateObjectRows,
            renderScopedRows,
            orderRows,
            orderStateRows,
            originalGeometryRows,
            originalStateRows,
            nonemptyStateIntervalRows,
            intervalsWithTwoAdjacentGeometryCandidates,
            intervalsWithOneAdjacentGeometryCandidate,
            intervalsWithZeroAdjacentGeometryCandidates,
            stateRowsJoined,
            stateRowsWithShapeTokenMatch,
            stateRowsWithCandidateAssetSource,
            candidateAssetValidRows,
            objectAssetJoinRows,
            objectsWithCompleteAmbiguityControl,
            originalInputStatusValidRows,
            orderInputStatusValidRows,
            stateAssetInputStatusValidRows,
            candidateAssetInputStatusValidRows,
            stateToGeometryUniqueBindingProvenRows,
            stateToMaterialUniqueBindingProvenRows,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalCandidateStreamRecordControlEvidence
        ReadOriginalCandidateStreamRecordControlEvidence(string path)
    {
        const string expectedHeader =
            "original_source_report\tcandidate_source_report\toriginal_rows\toriginal_primitive_rows\tcandidate_rows\tjoined_primitive_rows\tmissing_candidate_keys\tmissing_original_keys\tcandidate_complete_primitive_rows\tcandidate_incomplete_primitive_rows\tcandidate_slot_rows_materialized\tjoined_materialized_slot_rows\tsymbol_exact_rows\tauxiliary_exact_rows\tfetch_stride_exact_rows\tstream_stride_exact_rows\tbyte_length_exact_rows\telement_count_exact_rows\trecord_exact_rows\tcandidate_slot_records_unmaterialized_expected_rows\toriginal_input_valid_rows\tcandidate_input_valid_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var originalRows = ReadInt(row, "original_rows");
        var originalPrimitiveRows = ReadInt(row, "original_primitive_rows");
        var candidateRows = ReadInt(row, "candidate_rows");
        var joinedPrimitiveRows = ReadInt(row, "joined_primitive_rows");
        var missingCandidateKeys = ReadInt(row, "missing_candidate_keys");
        var missingOriginalKeys = ReadInt(row, "missing_original_keys");
        var candidateCompletePrimitiveRows = ReadInt(row, "candidate_complete_primitive_rows");
        var candidateIncompletePrimitiveRows = ReadInt(row, "candidate_incomplete_primitive_rows");
        var candidateSlotRowsMaterialized = ReadInt(row, "candidate_slot_rows_materialized");
        var joinedMaterializedSlotRows = ReadInt(row, "joined_materialized_slot_rows");
        var symbolExactRows = ReadInt(row, "symbol_exact_rows");
        var auxiliaryExactRows = ReadInt(row, "auxiliary_exact_rows");
        var fetchStrideExactRows = ReadInt(row, "fetch_stride_exact_rows");
        var streamStrideExactRows = ReadInt(row, "stream_stride_exact_rows");
        var byteLengthExactRows = ReadInt(row, "byte_length_exact_rows");
        var elementCountExactRows = ReadInt(row, "element_count_exact_rows");
        var recordExactRows = ReadInt(row, "record_exact_rows");
        var candidateSlotRecordsUnmaterializedExpectedRows = ReadInt(row, "candidate_slot_records_unmaterialized_expected_rows");
        var originalInputValidRows = ReadInt(row, "original_input_valid_rows");
        var candidateInputValidRows = ReadInt(row, "candidate_input_valid_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-candidate-stream-record-control", StringComparison.Ordinal) &&
                    originalRows == 11820 &&
                    originalPrimitiveRows == 2364 &&
                    candidateRows == 218 &&
                    joinedPrimitiveRows == 218 &&
                    missingCandidateKeys == 2146 &&
                    missingOriginalKeys == 0 &&
                    candidateCompletePrimitiveRows == 216 &&
                    candidateIncompletePrimitiveRows == 2 &&
                    candidateSlotRowsMaterialized == 1080 &&
                    joinedMaterializedSlotRows == 1080 &&
                    symbolExactRows == 1080 &&
                    auxiliaryExactRows == 1080 &&
                    fetchStrideExactRows == 1080 &&
                    streamStrideExactRows == 1080 &&
                    byteLengthExactRows == 1080 &&
                    elementCountExactRows == 1080 &&
                    recordExactRows == 1080 &&
                    candidateSlotRecordsUnmaterializedExpectedRows == 2 &&
                    originalInputValidRows == 218 &&
                    candidateInputValidRows == 218 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            originalRows,
            originalPrimitiveRows,
            candidateRows,
            joinedPrimitiveRows,
            missingCandidateKeys,
            missingOriginalKeys,
            candidateCompletePrimitiveRows,
            candidateIncompletePrimitiveRows,
            candidateSlotRowsMaterialized,
            joinedMaterializedSlotRows,
            symbolExactRows,
            auxiliaryExactRows,
            fetchStrideExactRows,
            streamStrideExactRows,
            byteLengthExactRows,
            elementCountExactRows,
            recordExactRows,
            candidateSlotRecordsUnmaterializedExpectedRows,
            originalInputValidRows,
            candidateInputValidRows,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalCandidateGeometryStreamControlEvidence
        ReadOriginalCandidateGeometryStreamControlEvidence(string path)
    {
        const string expectedHeader =
            "original_source_report\tcandidate_source_report\toriginal_slot_zero_rows\tcandidate_rows\tjoined_rows\tmissing_candidate_keys\tmissing_original_keys\tcandidate_source_slot_zero_rows\tcandidate_complete_rows\tcandidate_finite_and_indexed_rows\tsymbol_exact_rows\tstream_stride_exact_rows\tbyte_length_exact_rows\telement_count_exact_rows\tvertex_count_exact_rows\ttriangle_count_exact_rows\toriginal_input_valid_rows\tcandidate_input_valid_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var originalSlotZeroRows = ReadInt(row, "original_slot_zero_rows");
        var candidateRows = ReadInt(row, "candidate_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var missingCandidateKeys = ReadInt(row, "missing_candidate_keys");
        var missingOriginalKeys = ReadInt(row, "missing_original_keys");
        var candidateSourceSlotZeroRows = ReadInt(row, "candidate_source_slot_zero_rows");
        var candidateCompleteRows = ReadInt(row, "candidate_complete_rows");
        var candidateFiniteAndIndexedRows = ReadInt(row, "candidate_finite_and_indexed_rows");
        var symbolExactRows = ReadInt(row, "symbol_exact_rows");
        var streamStrideExactRows = ReadInt(row, "stream_stride_exact_rows");
        var byteLengthExactRows = ReadInt(row, "byte_length_exact_rows");
        var elementCountExactRows = ReadInt(row, "element_count_exact_rows");
        var vertexCountExactRows = ReadInt(row, "vertex_count_exact_rows");
        var triangleCountExactRows = ReadInt(row, "triangle_count_exact_rows");
        var originalInputValidRows = ReadInt(row, "original_input_valid_rows");
        var candidateInputValidRows = ReadInt(row, "candidate_input_valid_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-candidate-geometry-stream-control", StringComparison.Ordinal) &&
                    originalSlotZeroRows == 2364 &&
                    candidateRows == 38 &&
                    joinedRows == 38 &&
                    missingCandidateKeys == 2326 &&
                    missingOriginalKeys == 0 &&
                    candidateSourceSlotZeroRows == 38 &&
                    candidateCompleteRows == 38 &&
                    candidateFiniteAndIndexedRows == 38 &&
                    symbolExactRows == 38 &&
                    streamStrideExactRows == 38 &&
                    byteLengthExactRows == 38 &&
                    elementCountExactRows == 38 &&
                    vertexCountExactRows == 38 &&
                    triangleCountExactRows == 38 &&
                    originalInputValidRows == 38 &&
                    candidateInputValidRows == 38 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            originalSlotZeroRows,
            candidateRows,
            joinedRows,
            missingCandidateKeys,
            missingOriginalKeys,
            candidateSourceSlotZeroRows,
            candidateCompleteRows,
            candidateFiniteAndIndexedRows,
            symbolExactRows,
            streamStrideExactRows,
            byteLengthExactRows,
            elementCountExactRows,
            vertexCountExactRows,
            triangleCountExactRows,
            originalInputValidRows,
            candidateInputValidRows,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalCandidateGeometryPayloadIdentityEvidence
        ReadOriginalCandidateGeometryPayloadIdentityEvidence(string path)
    {
        const string expectedHeader =
            "topology_source_report\tcandidate_fetch_source_report\tcandidate_geometry_source_report\traw_identity_source_report\tgeometry_rows\tcandidate_geometry_rows\tjoined_rows\tmissing_candidate_geometry_keys\tmissing_candidate_fetch_keys\tmissing_raw_identity_keys\toriginal_slot_rows\tcandidate_slot_rows\traw_hash_rows\tsymbol_exact_rows\tmetadata_exact_rows\tpayload_hash_exact_rows\tcomplete_identity_rows\toriginal_input_valid_rows\tcandidate_input_valid_rows\tgeometry_input_valid_rows\traw_input_valid_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var geometryRows = ReadInt(row, "geometry_rows");
        var candidateGeometryRows = ReadInt(row, "candidate_geometry_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var missingCandidateGeometryKeys = ReadInt(row, "missing_candidate_geometry_keys");
        var missingCandidateFetchKeys = ReadInt(row, "missing_candidate_fetch_keys");
        var missingRawIdentityKeys = ReadInt(row, "missing_raw_identity_keys");
        var originalSlotRows = ReadInt(row, "original_slot_rows");
        var candidateSlotRows = ReadInt(row, "candidate_slot_rows");
        var rawHashRows = ReadInt(row, "raw_hash_rows");
        var symbolExactRows = ReadInt(row, "symbol_exact_rows");
        var metadataExactRows = ReadInt(row, "metadata_exact_rows");
        var payloadHashExactRows = ReadInt(row, "payload_hash_exact_rows");
        var completeIdentityRows = ReadInt(row, "complete_identity_rows");
        var originalInputValidRows = ReadInt(row, "original_input_valid_rows");
        var candidateInputValidRows = ReadInt(row, "candidate_input_valid_rows");
        var geometryInputValidRows = ReadInt(row, "geometry_input_valid_rows");
        var rawInputValidRows = ReadInt(row, "raw_input_valid_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-candidate-geometry-payload-identity-control", StringComparison.Ordinal) &&
                    geometryRows == 38 &&
                    candidateGeometryRows == 38 &&
                    joinedRows == 38 &&
                    missingCandidateGeometryKeys == 0 &&
                    missingCandidateFetchKeys == 0 &&
                    missingRawIdentityKeys == 0 &&
                    originalSlotRows == 190 &&
                    candidateSlotRows == 190 &&
                    rawHashRows == 190 &&
                    symbolExactRows == 190 &&
                    metadataExactRows == 190 &&
                    payloadHashExactRows == 190 &&
                    completeIdentityRows == 38 &&
                    originalInputValidRows == 38 &&
                    candidateInputValidRows == 38 &&
                    geometryInputValidRows == 38 &&
                    rawInputValidRows == 38 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            geometryRows,
            candidateGeometryRows,
            joinedRows,
            missingCandidateGeometryKeys,
            missingCandidateFetchKeys,
            missingRawIdentityKeys,
            originalSlotRows,
            candidateSlotRows,
            rawHashRows,
            symbolExactRows,
            metadataExactRows,
            payloadHashExactRows,
            completeIdentityRows,
            originalInputValidRows,
            candidateInputValidRows,
            geometryInputValidRows,
            rawInputValidRows,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalPrimitiveStreamTopologyEvidence ReadOriginalPrimitiveStreamTopologyEvidence(string path)
    {
        const string expectedHeader =
            "model_archive_path\tmodel_package_candidates\tmodel_packages_loaded\tobject_candidates\tobjects_loaded\tobjects_with_validated_streams\tprimitive_rows\tstream_rows\tprimitive_rows_with_ordinal_matched_streams\tstream_rows_with_ordinal_match\tstream_rows_with_stride_match\tstream_rows_with_valid_payload_range\tcomplete_primitive_rows\tobjects_with_non_overlapping_payload_ranges\treversed_slot_match_rows\tshifted_ordinal_match_rows\trejected_packages\trejected_objects\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var modelPackageCandidates = ReadInt(row, "model_package_candidates");
        var modelPackagesLoaded = ReadInt(row, "model_packages_loaded");
        var objectCandidates = ReadInt(row, "object_candidates");
        var objectsLoaded = ReadInt(row, "objects_loaded");
        var objectsWithValidatedStreams = ReadInt(row, "objects_with_validated_streams");
        var primitiveRows = ReadInt(row, "primitive_rows");
        var streamRows = ReadInt(row, "stream_rows");
        var primitiveRowsWithOrdinalMatchedStreams = ReadInt(row, "primitive_rows_with_ordinal_matched_streams");
        var streamRowsWithOrdinalMatch = ReadInt(row, "stream_rows_with_ordinal_match");
        var streamRowsWithStrideMatch = ReadInt(row, "stream_rows_with_stride_match");
        var streamRowsWithValidPayloadRange = ReadInt(row, "stream_rows_with_valid_payload_range");
        var completePrimitiveRows = ReadInt(row, "complete_primitive_rows");
        var objectsWithNonOverlappingPayloadRanges = ReadInt(row, "objects_with_non_overlapping_payload_ranges");
        var reversedSlotMatchRows = ReadInt(row, "reversed_slot_match_rows");
        var shiftedOrdinalMatchRows = ReadInt(row, "shifted_ordinal_match_rows");
        var rejectedPackages = ReadInt(row, "rejected_packages");
        var rejectedObjects = ReadInt(row, "rejected_objects");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, EaglOriginalPrimitiveStreamTopologyAudit.Status, StringComparison.Ordinal) &&
                    modelPackageCandidates > 0 &&
                    modelPackagesLoaded == modelPackageCandidates &&
                    objectCandidates > 0 &&
                    objectsLoaded > 0 &&
                    objectsWithValidatedStreams == objectsLoaded &&
                    primitiveRows > 0 &&
                    streamRows > 0 &&
                    primitiveRowsWithOrdinalMatchedStreams == primitiveRows &&
                    streamRowsWithOrdinalMatch == streamRows &&
                    streamRowsWithStrideMatch > 0 &&
                    streamRowsWithValidPayloadRange == streamRows &&
                    completePrimitiveRows > 0 &&
                    objectsWithNonOverlappingPayloadRanges == objectsLoaded &&
                    reversedSlotMatchRows == 0 &&
                    shiftedOrdinalMatchRows > 0 &&
                    rejectedPackages == 0 &&
                    rejectedObjects == 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            modelPackageCandidates,
            modelPackagesLoaded,
            objectCandidates,
            objectsLoaded,
            objectsWithValidatedStreams,
            primitiveRows,
            streamRows,
            primitiveRowsWithOrdinalMatchedStreams,
            streamRowsWithOrdinalMatch,
            streamRowsWithStrideMatch,
            streamRowsWithValidPayloadRange,
            completePrimitiveRows,
            objectsWithNonOverlappingPayloadRanges,
            reversedSlotMatchRows,
            shiftedOrdinalMatchRows,
            rejectedPackages,
            rejectedObjects,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalPrimitiveStreamOpaqueDiscriminationEvidence ReadOriginalPrimitiveStreamOpaqueDiscriminationEvidence(
        string path,
        string controlPath)
    {
        const string expectedHeader =
            "model_archive_path\tmodel_package_candidates\tmodel_packages_loaded\tobject_candidates\tobjects_loaded\tobjects_with_validated_streams\tprimitive_rows\tcomplete_fetch_rows\tcandidate_ordinal_rows\tstride_matching_candidate_rows\tmetadata_matching_candidate_rows\tpayload_prefix_matching_candidate_rows\tcurrent_ordinal_rows\tcurrent_ordinal_metadata_matching_rows\tcurrent_ordinal_payload_prefix_matching_rows\tuniquely_discriminated_by_metadata_rows\tuniquely_discriminated_by_payload_prefix_rows\tshifted_stride_candidate_rows\tshifted_metadata_matching_rows\tshifted_payload_prefix_matching_rows\trejected_packages\trejected_objects\tstatus\tprovenance\toriginal_semantics";
        const string expectedControlHeader =
            "source_report\tinput_rows\tprimitive_rows\tcomplete_fetch_rows\tcandidate_ordinal_rows\tstride_matching_candidate_rows\tmetadata_matching_candidate_rows\tpayload_prefix_matching_candidate_rows\tcurrent_ordinal_rows\tcurrent_ordinal_metadata_matching_rows\tcurrent_ordinal_payload_prefix_matching_rows\tuniquely_discriminated_by_metadata_rows\tuniquely_discriminated_by_payload_prefix_rows\tshifted_stride_candidate_rows\tshifted_metadata_matching_rows\tshifted_payload_prefix_matching_rows\tinput_status_valid_rows\tcandidate_list_coherent_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var controlRows = ReadTsvRows(controlPath);
        var controlRow = controlRows.FirstOrDefault();
        var primitiveRows = ReadInt(row, "primitive_rows");
        var completeFetchRows = ReadInt(row, "complete_fetch_rows");
        var candidateOrdinalRows = ReadInt(row, "candidate_ordinal_rows");
        var strideMatchingCandidateRows = ReadInt(row, "stride_matching_candidate_rows");
        var metadataMatchingCandidateRows = ReadInt(row, "metadata_matching_candidate_rows");
        var payloadPrefixMatchingCandidateRows = ReadInt(row, "payload_prefix_matching_candidate_rows");
        var currentOrdinalRows = ReadInt(row, "current_ordinal_rows");
        var currentOrdinalMetadataMatchingRows = ReadInt(row, "current_ordinal_metadata_matching_rows");
        var currentOrdinalPayloadPrefixMatchingRows = ReadInt(row, "current_ordinal_payload_prefix_matching_rows");
        var uniquelyDiscriminatedByMetadataRows = ReadInt(row, "uniquely_discriminated_by_metadata_rows");
        var uniquelyDiscriminatedByPayloadPrefixRows = ReadInt(row, "uniquely_discriminated_by_payload_prefix_rows");
        var shiftedStrideCandidateRows = ReadInt(row, "shifted_stride_candidate_rows");
        var shiftedMetadataMatchingRows = ReadInt(row, "shifted_metadata_matching_rows");
        var shiftedPayloadPrefixMatchingRows = ReadInt(row, "shifted_payload_prefix_matching_rows");
        var rejectedPackages = ReadInt(row, "rejected_packages");
        var rejectedObjects = ReadInt(row, "rejected_objects");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var controlStatus = Get(controlRow, "status");
        var controlProvenance = Get(controlRow, "provenance");
        var valid = HasHeader(path, expectedHeader) &&
                    HasHeader(controlPath, expectedControlHeader) &&
                    string.Equals(status, EaglOriginalPrimitiveStreamOpaqueDiscriminationAudit.Status, StringComparison.Ordinal) &&
                    string.Equals(controlStatus, "verified-independent-original-primitive-stream-opaque-discrimination-control", StringComparison.Ordinal) &&
                    primitiveRows > 0 &&
                    completeFetchRows > 0 &&
                    candidateOrdinalRows > 0 &&
                    strideMatchingCandidateRows > 0 &&
                    metadataMatchingCandidateRows == 0 &&
                    payloadPrefixMatchingCandidateRows == 0 &&
                    currentOrdinalRows == primitiveRows &&
                    currentOrdinalMetadataMatchingRows == 0 &&
                    currentOrdinalPayloadPrefixMatchingRows == 0 &&
                    uniquelyDiscriminatedByMetadataRows == 0 &&
                    uniquelyDiscriminatedByPayloadPrefixRows == 0 &&
                    shiftedStrideCandidateRows > 0 &&
                    shiftedMetadataMatchingRows == 0 &&
                    shiftedPayloadPrefixMatchingRows == 0 &&
                    rejectedPackages == 0 &&
                    rejectedObjects == 0 &&
                    ReadInt(controlRow, "input_rows") == primitiveRows &&
                    ReadInt(controlRow, "primitive_rows") == primitiveRows &&
                    ReadInt(controlRow, "complete_fetch_rows") == completeFetchRows &&
                    ReadInt(controlRow, "candidate_ordinal_rows") == candidateOrdinalRows &&
                    ReadInt(controlRow, "stride_matching_candidate_rows") == strideMatchingCandidateRows &&
                    ReadInt(controlRow, "metadata_matching_candidate_rows") == 0 &&
                    ReadInt(controlRow, "payload_prefix_matching_candidate_rows") == 0 &&
                    ReadInt(controlRow, "current_ordinal_rows") == primitiveRows &&
                    ReadInt(controlRow, "shifted_stride_candidate_rows") == shiftedStrideCandidateRows &&
                    ReadInt(controlRow, "shifted_metadata_matching_rows") == 0 &&
                    ReadInt(controlRow, "shifted_payload_prefix_matching_rows") == 0 &&
                    ReadInt(controlRow, "input_status_valid_rows") == primitiveRows &&
                    ReadInt(controlRow, "candidate_list_coherent_rows") == primitiveRows &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    !string.IsNullOrWhiteSpace(controlProvenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            primitiveRows,
            completeFetchRows,
            candidateOrdinalRows,
            strideMatchingCandidateRows,
            metadataMatchingCandidateRows,
            payloadPrefixMatchingCandidateRows,
            currentOrdinalRows,
            currentOrdinalMetadataMatchingRows,
            currentOrdinalPayloadPrefixMatchingRows,
            uniquelyDiscriminatedByMetadataRows,
            uniquelyDiscriminatedByPayloadPrefixRows,
            shiftedStrideCandidateRows,
            shiftedMetadataMatchingRows,
            shiftedPayloadPrefixMatchingRows,
            rejectedPackages,
            rejectedObjects,
            status,
            provenance,
            originalSemantics,
            controlStatus,
            controlProvenance);
    }

    private static OriginalPrimitiveStreamCommandPayloadIdentityEvidence ReadOriginalPrimitiveStreamCommandPayloadIdentityEvidence(
        string path,
        string controlPath)
    {
        const string expectedHeader =
            "model_archive_path\tmodel_package_candidates\tmodel_packages_loaded\tobject_candidates\tobjects_loaded\tobjects_with_validated_streams\tprimitive_rows\tcomplete_fetch_rows\tcandidate_ordinal_rows\traw_identity_candidate_rows\tdistinct_raw_identity_rows\tduplicate_raw_identity_rows\tcommand_payload_exact_candidate_rows\tcurrent_ordinal_rows\tcurrent_ordinal_raw_identity_unique_rows\tcurrent_ordinal_command_payload_exact_rows\tcurrent_ordinal_uniquely_discriminated_by_command_payload_rows\tshifted_ordinal_command_payload_exact_rows\trejected_packages\trejected_objects\tstatus\tprovenance\toriginal_semantics";
        const string expectedControlHeader =
            "source_report\tinput_rows\tprimitive_rows\tcomplete_fetch_rows\tcandidate_ordinal_rows\traw_identity_candidate_rows\tdistinct_raw_identity_rows\tduplicate_raw_identity_rows\tcommand_payload_exact_candidate_rows\tcurrent_ordinal_rows\tcurrent_ordinal_raw_identity_unique_rows\tcurrent_ordinal_command_payload_exact_rows\tcurrent_ordinal_uniquely_discriminated_by_command_payload_rows\tshifted_ordinal_command_payload_exact_rows\tinput_status_valid_rows\tcandidate_list_coherent_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var controlRows = ReadTsvRows(controlPath);
        var controlRow = controlRows.FirstOrDefault();
        var primitiveRows = ReadInt(row, "primitive_rows");
        var completeFetchRows = ReadInt(row, "complete_fetch_rows");
        var candidateOrdinalRows = ReadInt(row, "candidate_ordinal_rows");
        var rawIdentityCandidateRows = ReadInt(row, "raw_identity_candidate_rows");
        var distinctRawIdentityRows = ReadInt(row, "distinct_raw_identity_rows");
        var duplicateRawIdentityRows = ReadInt(row, "duplicate_raw_identity_rows");
        var commandPayloadExactCandidateRows = ReadInt(row, "command_payload_exact_candidate_rows");
        var currentOrdinalRows = ReadInt(row, "current_ordinal_rows");
        var currentOrdinalRawIdentityUniqueRows = ReadInt(row, "current_ordinal_raw_identity_unique_rows");
        var currentOrdinalCommandPayloadExactRows = ReadInt(row, "current_ordinal_command_payload_exact_rows");
        var currentOrdinalUniquelyDiscriminatedByCommandPayloadRows =
            ReadInt(row, "current_ordinal_uniquely_discriminated_by_command_payload_rows");
        var shiftedOrdinalCommandPayloadExactRows =
            ReadInt(row, "shifted_ordinal_command_payload_exact_rows");
        var rejectedPackages = ReadInt(row, "rejected_packages");
        var rejectedObjects = ReadInt(row, "rejected_objects");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var controlStatus = Get(controlRow, "status");
        var controlProvenance = Get(controlRow, "provenance");
        var valid = HasHeader(path, expectedHeader) &&
                    HasHeader(controlPath, expectedControlHeader) &&
                    string.Equals(status, EaglOriginalPrimitiveStreamCommandPayloadIdentityAudit.Status, StringComparison.Ordinal) &&
                    string.Equals(controlStatus, "verified-independent-original-primitive-stream-command-payload-identity-control", StringComparison.Ordinal) &&
                    primitiveRows > 0 &&
                    completeFetchRows > 0 &&
                    candidateOrdinalRows > 0 &&
                    rawIdentityCandidateRows == candidateOrdinalRows &&
                    distinctRawIdentityRows == rawIdentityCandidateRows &&
                    duplicateRawIdentityRows == 0 &&
                    commandPayloadExactCandidateRows == 0 &&
                    currentOrdinalRows == primitiveRows &&
                    currentOrdinalRawIdentityUniqueRows == primitiveRows &&
                    currentOrdinalCommandPayloadExactRows == 0 &&
                    currentOrdinalUniquelyDiscriminatedByCommandPayloadRows == 0 &&
                    shiftedOrdinalCommandPayloadExactRows == 0 &&
                    rejectedPackages == 0 &&
                    rejectedObjects == 0 &&
                    ReadInt(controlRow, "input_rows") == primitiveRows &&
                    ReadInt(controlRow, "primitive_rows") == primitiveRows &&
                    ReadInt(controlRow, "complete_fetch_rows") == completeFetchRows &&
                    ReadInt(controlRow, "candidate_ordinal_rows") == candidateOrdinalRows &&
                    ReadInt(controlRow, "raw_identity_candidate_rows") == rawIdentityCandidateRows &&
                    ReadInt(controlRow, "distinct_raw_identity_rows") == distinctRawIdentityRows &&
                    ReadInt(controlRow, "duplicate_raw_identity_rows") == 0 &&
                    ReadInt(controlRow, "command_payload_exact_candidate_rows") == 0 &&
                    ReadInt(controlRow, "current_ordinal_rows") == currentOrdinalRows &&
                    ReadInt(controlRow, "current_ordinal_raw_identity_unique_rows") == currentOrdinalRawIdentityUniqueRows &&
                    ReadInt(controlRow, "current_ordinal_command_payload_exact_rows") == 0 &&
                    ReadInt(controlRow, "current_ordinal_uniquely_discriminated_by_command_payload_rows") == 0 &&
                    ReadInt(controlRow, "shifted_ordinal_command_payload_exact_rows") == 0 &&
                    ReadInt(controlRow, "input_status_valid_rows") == primitiveRows &&
                    ReadInt(controlRow, "candidate_list_coherent_rows") == primitiveRows &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    !string.IsNullOrWhiteSpace(controlProvenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            primitiveRows,
            completeFetchRows,
            candidateOrdinalRows,
            rawIdentityCandidateRows,
            distinctRawIdentityRows,
            duplicateRawIdentityRows,
            commandPayloadExactCandidateRows,
            currentOrdinalRows,
            currentOrdinalRawIdentityUniqueRows,
            currentOrdinalCommandPayloadExactRows,
            currentOrdinalUniquelyDiscriminatedByCommandPayloadRows,
            shiftedOrdinalCommandPayloadExactRows,
            rejectedPackages,
            rejectedObjects,
            status,
            provenance,
            originalSemantics,
            controlStatus,
            controlProvenance);
    }

    private static OriginalStateFetchAdjacencyControlEvidence
        ReadOriginalStateFetchAdjacencyControlEvidence(string path)
    {
        const string expectedHeader =
            "render_source_report\tadjacency_source_report\trender_rows\tadjacency_rows\tjoined_rows\trender_input_status_valid_rows\tadjacency_input_status_valid_rows\tstate_relocation_rows\tshapename_rows\tprimitive_type_rows\tcomplete_five_slot_fetch_rows\treported_state_fetch_overlap_rows\trecomputed_state_fetch_overlap_rows\treported_state_shape_primitive_type_overlap_rows\trecomputed_state_shape_primitive_type_overlap_rows\tjoined_exact_adjacency_rows\tjoined_state_exact_adjacency_rows\tjoined_state_fetch_exact_adjacency_rows\tjoined_shapename_exact_adjacency_rows\tjoined_primitive_type_exact_adjacency_rows\texact_adjacency_category_histogram\tstate_exact_adjacency_category_histogram\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var renderRows = ReadInt(row, "render_rows");
        var adjacencyRows = ReadInt(row, "adjacency_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var stateRelocationRows = ReadInt(row, "state_relocation_rows");
        var shapeNameRows = ReadInt(row, "shapename_rows");
        var primitiveTypeRows = ReadInt(row, "primitive_type_rows");
        var completeFiveSlotFetchRows = ReadInt(row, "complete_five_slot_fetch_rows");
        var reportedStateFetchOverlapRows = ReadInt(row, "reported_state_fetch_overlap_rows");
        var recomputedStateFetchOverlapRows = ReadInt(row, "recomputed_state_fetch_overlap_rows");
        var joinedExactAdjacencyRows = ReadInt(row, "joined_exact_adjacency_rows");
        var joinedStateExactAdjacencyRows = ReadInt(row, "joined_state_exact_adjacency_rows");
        var joinedStateFetchExactAdjacencyRows = ReadInt(row, "joined_state_fetch_exact_adjacency_rows");
        var exactAdjacencyCategoryHistogram = Get(row, "exact_adjacency_category_histogram");
        var stateExactAdjacencyCategoryHistogram = Get(row, "state_exact_adjacency_category_histogram");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-fetch-adjacency-control", StringComparison.Ordinal) &&
                    renderRows > 0 &&
                    adjacencyRows == renderRows &&
                    joinedRows == renderRows &&
                    ReadInt(row, "render_input_status_valid_rows") == renderRows &&
                    ReadInt(row, "adjacency_input_status_valid_rows") == adjacencyRows &&
                    stateRelocationRows == renderRows &&
                    joinedExactAdjacencyRows == renderRows &&
                    reportedStateFetchOverlapRows == recomputedStateFetchOverlapRows &&
                    !string.IsNullOrWhiteSpace(exactAdjacencyCategoryHistogram) &&
                    !string.IsNullOrWhiteSpace(stateExactAdjacencyCategoryHistogram) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            renderRows,
            adjacencyRows,
            joinedRows,
            stateRelocationRows,
            shapeNameRows,
            primitiveTypeRows,
            completeFiveSlotFetchRows,
            reportedStateFetchOverlapRows,
            recomputedStateFetchOverlapRows,
            joinedExactAdjacencyRows,
            joinedStateExactAdjacencyRows,
            joinedStateFetchExactAdjacencyRows,
            exactAdjacencyCategoryHistogram,
            stateExactAdjacencyCategoryHistogram,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalPrimitiveFetchTopologyControlEvidence
        ReadOriginalPrimitiveFetchTopologyControlEvidence(string path)
    {
        const string expectedHeader =
            "render_source_report\ttopology_source_report\trender_rows\ttopology_rows\tjoined_rows\trender_input_status_valid_rows\ttopology_input_status_valid_rows\ttopology_groups_with_five_rows\ttopology_valid_slot_rows\ttopology_key_ordinal_match_rows\ttopology_valid_payload_range_rows\ttopology_structural_match_rows\trender_fetch_slot_rows\trender_complete_five_slot_rows\ttopology_complete_five_slot_rows\tfetch_slot_count_match_rows\tfetch_slot_id_set_match_rows\tcomplete_five_slot_sequence_agreement_rows\tincomplete_slot_two_rows\tmissing_render_keys\tmissing_topology_keys\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var renderRows = ReadInt(row, "render_rows");
        var topologyRows = ReadInt(row, "topology_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var renderInputStatusValidRows = ReadInt(row, "render_input_status_valid_rows");
        var topologyInputStatusValidRows = ReadInt(row, "topology_input_status_valid_rows");
        var topologyGroupsWithFiveRows = ReadInt(row, "topology_groups_with_five_rows");
        var topologyValidSlotRows = ReadInt(row, "topology_valid_slot_rows");
        var topologyKeyOrdinalMatchRows = ReadInt(row, "topology_key_ordinal_match_rows");
        var topologyValidPayloadRangeRows = ReadInt(row, "topology_valid_payload_range_rows");
        var topologyStructuralMatchRows = ReadInt(row, "topology_structural_match_rows");
        var renderFetchSlotRows = ReadInt(row, "render_fetch_slot_rows");
        var renderCompleteFiveSlotRows = ReadInt(row, "render_complete_five_slot_rows");
        var topologyCompleteFiveSlotRows = ReadInt(row, "topology_complete_five_slot_rows");
        var fetchSlotCountMatchRows = ReadInt(row, "fetch_slot_count_match_rows");
        var fetchSlotIdSetMatchRows = ReadInt(row, "fetch_slot_id_set_match_rows");
        var completeFiveSlotSequenceAgreementRows = ReadInt(row, "complete_five_slot_sequence_agreement_rows");
        var incompleteSlotTwoRows = ReadInt(row, "incomplete_slot_two_rows");
        var missingRenderKeys = ReadInt(row, "missing_render_keys");
        var missingTopologyKeys = ReadInt(row, "missing_topology_keys");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-primitive-fetch-topology-control", StringComparison.Ordinal) &&
                    renderRows > 0 &&
                    topologyRows > 0 &&
                    joinedRows == renderRows &&
                    renderInputStatusValidRows == renderRows &&
                    topologyInputStatusValidRows == topologyRows &&
                    topologyGroupsWithFiveRows == renderRows &&
                    topologyKeyOrdinalMatchRows == topologyRows &&
                    topologyValidPayloadRangeRows == topologyRows &&
                    topologyStructuralMatchRows > 0 &&
                    renderFetchSlotRows == topologyValidSlotRows &&
                    renderCompleteFiveSlotRows == topologyCompleteFiveSlotRows &&
                    fetchSlotCountMatchRows == renderRows &&
                    fetchSlotIdSetMatchRows == renderRows &&
                    completeFiveSlotSequenceAgreementRows == renderRows &&
                    incompleteSlotTwoRows >= 0 &&
                    missingRenderKeys == 0 &&
                    missingTopologyKeys == 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            renderRows,
            topologyRows,
            joinedRows,
            renderInputStatusValidRows,
            topologyInputStatusValidRows,
            topologyGroupsWithFiveRows,
            topologyValidSlotRows,
            topologyKeyOrdinalMatchRows,
            topologyValidPayloadRangeRows,
            topologyStructuralMatchRows,
            renderFetchSlotRows,
            renderCompleteFiveSlotRows,
            topologyCompleteFiveSlotRows,
            fetchSlotCountMatchRows,
            fetchSlotIdSetMatchRows,
            completeFiveSlotSequenceAgreementRows,
            incompleteSlotTwoRows,
            missingRenderKeys,
            missingTopologyKeys,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalCommandPayloadRegionControlEvidence
        ReadOriginalCommandPayloadRegionControlEvidence(string path)
    {
        const string expectedHeader =
            "render_source_report\ttopology_source_report\trender_rows\ttopology_rows\tjoined_rows\trender_input_status_valid_rows\ttopology_input_status_valid_rows\ttopology_groups_with_five_rows\tpayload_range_valid_rows\tpayload_interval_nonoverlap_rows\tcommand_section_match_rows\tcommand_interval_valid_rows\tcommand_payload_nonoverlap_rows\tcommand_after_payload_end_rows\tcommand_payload_gap_equals_0x10_rows\tcommand_payload_gap_histogram\tincomplete_slot_two_rows\tmissing_render_keys\tmissing_topology_keys\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var renderRows = ReadInt(row, "render_rows");
        var topologyRows = ReadInt(row, "topology_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var renderInputStatusValidRows = ReadInt(row, "render_input_status_valid_rows");
        var topologyInputStatusValidRows = ReadInt(row, "topology_input_status_valid_rows");
        var topologyGroupsWithFiveRows = ReadInt(row, "topology_groups_with_five_rows");
        var payloadRangeValidRows = ReadInt(row, "payload_range_valid_rows");
        var payloadIntervalNonoverlapRows = ReadInt(row, "payload_interval_nonoverlap_rows");
        var commandSectionMatchRows = ReadInt(row, "command_section_match_rows");
        var commandIntervalValidRows = ReadInt(row, "command_interval_valid_rows");
        var commandPayloadNonoverlapRows = ReadInt(row, "command_payload_nonoverlap_rows");
        var commandAfterPayloadEndRows = ReadInt(row, "command_after_payload_end_rows");
        var commandPayloadGapEquals0x10Rows = ReadInt(row, "command_payload_gap_equals_0x10_rows");
        var commandPayloadGapHistogram = Get(row, "command_payload_gap_histogram");
        var incompleteSlotTwoRows = ReadInt(row, "incomplete_slot_two_rows");
        var missingRenderKeys = ReadInt(row, "missing_render_keys");
        var missingTopologyKeys = ReadInt(row, "missing_topology_keys");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-command-payload-region-control", StringComparison.Ordinal) &&
                    renderRows > 0 &&
                    topologyRows > 0 &&
                    joinedRows == renderRows &&
                    renderInputStatusValidRows == renderRows &&
                    topologyInputStatusValidRows == topologyRows &&
                    topologyGroupsWithFiveRows == renderRows &&
                    payloadRangeValidRows == renderRows &&
                    payloadIntervalNonoverlapRows == renderRows &&
                    commandSectionMatchRows == renderRows &&
                    commandIntervalValidRows == renderRows &&
                    commandPayloadNonoverlapRows == renderRows &&
                    commandAfterPayloadEndRows == renderRows &&
                    commandPayloadGapEquals0x10Rows == renderRows &&
                    !string.IsNullOrWhiteSpace(commandPayloadGapHistogram) &&
                    incompleteSlotTwoRows >= 0 &&
                    missingRenderKeys == 0 &&
                    missingTopologyKeys == 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            renderRows,
            topologyRows,
            joinedRows,
            renderInputStatusValidRows,
            topologyInputStatusValidRows,
            topologyGroupsWithFiveRows,
            payloadRangeValidRows,
            payloadIntervalNonoverlapRows,
            commandSectionMatchRows,
            commandIntervalValidRows,
            commandPayloadNonoverlapRows,
            commandAfterPayloadEndRows,
            commandPayloadGapEquals0x10Rows,
            commandPayloadGapHistogram,
            incompleteSlotTwoRows,
            missingRenderKeys,
            missingTopologyKeys,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalPrimitiveStreamCommandDescriptorAdjacencyEvidence
        ReadOriginalPrimitiveStreamCommandDescriptorAdjacencyEvidence(
            string path,
            string controlPath)
    {
        const string expectedHeader =
            "model_archive_path\tcommand_descriptor_delta\tmodel_package_candidates\tmodel_packages_loaded\tobject_candidates\tobjects_loaded\tobjects_with_validated_streams\tprimitive_rows\tcomplete_command_rows\tcandidate_ordinal_rows\tidentity_candidate_rows\tcurrent_ordinal_rows\tcurrent_ordinal_identity_rows\tunique_current_ordinal_identity_rows\tshifted_ordinal_identity_rows\tsame_section_identity_rows\texact_adjacency_stream_rows\texact_adjacency_category_histogram\texact_adjacency_stride_histogram\trejected_packages\trejected_objects\tstatus\tprovenance\toriginal_semantics";
        const string expectedControlHeader =
            "render_source_report\ttopology_source_report\tcommand_descriptor_delta\trender_rows\ttopology_rows\tprimitive_rows\tcomplete_command_rows\tcandidate_ordinal_rows\tidentity_candidate_rows\tcurrent_ordinal_rows\tcurrent_ordinal_identity_rows\tunique_current_ordinal_identity_rows\tshifted_ordinal_identity_rows\tsame_section_identity_rows\texact_adjacency_stream_rows\texact_adjacency_category_histogram\texact_adjacency_stride_histogram\trender_input_status_valid_rows\ttopology_input_status_valid_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var controlRows = ReadTsvRows(controlPath);
        var controlRow = controlRows.FirstOrDefault();
        var delta = checked((uint)ReadInt(row, "command_descriptor_delta"));
        var primitiveRows = ReadInt(row, "primitive_rows");
        var completeCommandRows = ReadInt(row, "complete_command_rows");
        var candidateOrdinalRows = ReadInt(row, "candidate_ordinal_rows");
        var identityCandidateRows = ReadInt(row, "identity_candidate_rows");
        var currentOrdinalRows = ReadInt(row, "current_ordinal_rows");
        var currentOrdinalIdentityRows = ReadInt(row, "current_ordinal_identity_rows");
        var uniqueCurrentOrdinalIdentityRows = ReadInt(row, "unique_current_ordinal_identity_rows");
        var shiftedOrdinalIdentityRows = ReadInt(row, "shifted_ordinal_identity_rows");
        var sameSectionIdentityRows = ReadInt(row, "same_section_identity_rows");
        var exactAdjacencyStreamRows = ReadInt(row, "exact_adjacency_stream_rows");
        var exactAdjacencyCategoryHistogram = Get(row, "exact_adjacency_category_histogram");
        var exactAdjacencyStrideHistogram = Get(row, "exact_adjacency_stride_histogram");
        var rejectedPackages = ReadInt(row, "rejected_packages");
        var rejectedObjects = ReadInt(row, "rejected_objects");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var controlStatus = Get(controlRow, "status");
        var controlProvenance = Get(controlRow, "provenance");
        var controlPrimitiveRows = ReadInt(controlRow, "primitive_rows");
        var controlCompleteCommandRows = ReadInt(controlRow, "complete_command_rows");
        var controlCandidateOrdinalRows = ReadInt(controlRow, "candidate_ordinal_rows");
        var controlIdentityCandidateRows = ReadInt(controlRow, "identity_candidate_rows");
        var controlCurrentOrdinalRows = ReadInt(controlRow, "current_ordinal_rows");
        var controlCurrentOrdinalIdentityRows = ReadInt(controlRow, "current_ordinal_identity_rows");
        var controlUniqueRows = ReadInt(controlRow, "unique_current_ordinal_identity_rows");
        var controlShiftedRows = ReadInt(controlRow, "shifted_ordinal_identity_rows");
        var controlSameSectionRows = ReadInt(controlRow, "same_section_identity_rows");
        var controlExactAdjacencyStreamRows = ReadInt(controlRow, "exact_adjacency_stream_rows");
        var controlExactAdjacencyCategoryHistogram = Get(controlRow, "exact_adjacency_category_histogram");
        var controlExactAdjacencyStrideHistogram = Get(controlRow, "exact_adjacency_stride_histogram");
        var valid = HasHeader(path, expectedHeader) &&
                    HasHeader(controlPath, expectedControlHeader) &&
                    string.Equals(status, EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyAudit.Status, StringComparison.Ordinal) &&
                    string.Equals(controlStatus, "verified-independent-original-primitive-stream-command-descriptor-adjacency-control", StringComparison.Ordinal) &&
                    delta == EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyAudit.CommandDescriptorDelta &&
                    primitiveRows > 0 &&
                    completeCommandRows == primitiveRows &&
                    candidateOrdinalRows > 0 &&
                    identityCandidateRows == primitiveRows &&
                    currentOrdinalRows == primitiveRows &&
                    currentOrdinalIdentityRows == primitiveRows &&
                    uniqueCurrentOrdinalIdentityRows == primitiveRows &&
                    shiftedOrdinalIdentityRows == 0 &&
                    sameSectionIdentityRows == primitiveRows &&
                    exactAdjacencyStreamRows > 0 &&
                    rejectedPackages == 0 &&
                    rejectedObjects == 0 &&
                    controlPrimitiveRows == primitiveRows &&
                    controlCompleteCommandRows == completeCommandRows &&
                    controlCandidateOrdinalRows == candidateOrdinalRows &&
                    controlIdentityCandidateRows == identityCandidateRows &&
                    controlCurrentOrdinalRows == currentOrdinalRows &&
                    controlCurrentOrdinalIdentityRows == currentOrdinalIdentityRows &&
                    controlUniqueRows == uniqueCurrentOrdinalIdentityRows &&
                    controlShiftedRows == shiftedOrdinalIdentityRows &&
                    controlSameSectionRows == sameSectionIdentityRows &&
                    controlExactAdjacencyStreamRows == exactAdjacencyStreamRows &&
                    string.Equals(controlExactAdjacencyCategoryHistogram, exactAdjacencyCategoryHistogram, StringComparison.Ordinal) &&
                    string.Equals(controlExactAdjacencyStrideHistogram, exactAdjacencyStrideHistogram, StringComparison.Ordinal) &&
                    ReadInt(controlRow, "render_input_status_valid_rows") == primitiveRows &&
                    ReadInt(controlRow, "topology_input_status_valid_rows") > 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    !string.IsNullOrWhiteSpace(controlProvenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            delta,
            primitiveRows,
            completeCommandRows,
            candidateOrdinalRows,
            identityCandidateRows,
            currentOrdinalRows,
            currentOrdinalIdentityRows,
            uniqueCurrentOrdinalIdentityRows,
            shiftedOrdinalIdentityRows,
            sameSectionIdentityRows,
            exactAdjacencyStreamRows,
            exactAdjacencyCategoryHistogram,
            exactAdjacencyStrideHistogram,
            rejectedPackages,
            rejectedObjects,
            status,
            provenance,
            originalSemantics,
            controlStatus,
            controlProvenance);
    }

    private static OriginalRenderPayloadObjectJoinEvidence ReadOriginalRenderPayloadObjectJoinEvidence(string path)
    {
        const string expectedHeader =
            "render_source_report\tpayload_source_report\trender_object_rows\tpayload_object_rows\tjoined_object_rows\trender_only_objects\tpayload_only_objects\trender_primitive_rows\tpayload_validated_stream_rows\tjoined_identity_match_rows\trender_status_valid_rows\tpayload_status_valid_rows\tstructural_counts_valid_rows\treversed_key_match_rows\tshifted_key_match_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var renderObjectRows = ReadInt(row, "render_object_rows");
        var payloadObjectRows = ReadInt(row, "payload_object_rows");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var renderOnlyObjects = ReadInt(row, "render_only_objects");
        var payloadOnlyObjects = ReadInt(row, "payload_only_objects");
        var renderPrimitiveRows = ReadInt(row, "render_primitive_rows");
        var payloadValidatedStreamRows = ReadInt(row, "payload_validated_stream_rows");
        var joinedIdentityMatchRows = ReadInt(row, "joined_identity_match_rows");
        var renderStatusValidRows = ReadInt(row, "render_status_valid_rows");
        var payloadStatusValidRows = ReadInt(row, "payload_status_valid_rows");
        var structuralCountsValidRows = ReadInt(row, "structural_counts_valid_rows");
        var reversedKeyMatchRows = ReadInt(row, "reversed_key_match_rows");
        var shiftedKeyMatchRows = ReadInt(row, "shifted_key_match_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-render-payload-object-join", StringComparison.Ordinal) &&
                    renderObjectRows > 0 &&
                    payloadObjectRows == renderObjectRows &&
                    joinedObjectRows == renderObjectRows &&
                    renderOnlyObjects == 0 &&
                    payloadOnlyObjects == 0 &&
                    renderPrimitiveRows > 0 &&
                    payloadValidatedStreamRows > 0 &&
                    joinedIdentityMatchRows == joinedObjectRows &&
                    renderStatusValidRows == joinedObjectRows &&
                    payloadStatusValidRows == joinedObjectRows &&
                    structuralCountsValidRows == joinedObjectRows &&
                    reversedKeyMatchRows == 0 &&
                    shiftedKeyMatchRows == 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            renderObjectRows,
            payloadObjectRows,
            joinedObjectRows,
            renderOnlyObjects,
            payloadOnlyObjects,
            renderPrimitiveRows,
            payloadValidatedStreamRows,
            joinedIdentityMatchRows,
            renderStatusValidRows,
            payloadStatusValidRows,
            structuralCountsValidRows,
            reversedKeyMatchRows,
            shiftedKeyMatchRows,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalStateGeometryPartitionControlEvidence
        ReadOriginalStateGeometryPartitionControlEvidence(string path)
    {
        const string expectedHeader =
            "adjacency_source_report\tprofile_source_report\torder_source_report\tadjacency_rows\tprofile_rows\torder_rows\tjoined_object_rows\tadjacency_input_status_valid_rows\tprofile_input_status_valid_rows\torder_input_status_valid_rows\tobject_state_exact_rows\tgeometry_group_state_exact_rows\tprofile_geometry_pair_rows\tprofile_state_pair_rows\tprofile_rows_reconciled\torder_partitioned_states\tadjacency_states\torder_states\tstate_index_set_match_rows\tinterval_index_sequence_match_rows\tinterval_boundary_shape_match_rows\tcomplete_profile_stream_rows\texact_command_shape_rows\texact_command_shape_expected_zero\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var adjacencyRows = ReadInt(row, "adjacency_rows");
        var profileRows = ReadInt(row, "profile_rows");
        var orderRows = ReadInt(row, "order_rows");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var adjacencyInputStatusValidRows = ReadInt(row, "adjacency_input_status_valid_rows");
        var profileInputStatusValidRows = ReadInt(row, "profile_input_status_valid_rows");
        var orderInputStatusValidRows = ReadInt(row, "order_input_status_valid_rows");
        var objectStateExactRows = ReadInt(row, "object_state_exact_rows");
        var geometryGroupStateExactRows = ReadInt(row, "geometry_group_state_exact_rows");
        var profileGeometryPairRows = ReadInt(row, "profile_geometry_pair_rows");
        var profileStatePairRows = ReadInt(row, "profile_state_pair_rows");
        var profileRowsReconciled = ReadInt(row, "profile_rows_reconciled");
        var orderPartitionedStates = ReadInt(row, "order_partitioned_states");
        var adjacencyStates = ReadInt(row, "adjacency_states");
        var orderStates = ReadInt(row, "order_states");
        var stateIndexSetMatchRows = ReadInt(row, "state_index_set_match_rows");
        var intervalIndexSequenceMatchRows = ReadInt(row, "interval_index_sequence_match_rows");
        var intervalBoundaryShapeMatchRows = ReadInt(row, "interval_boundary_shape_match_rows");
        var completeProfileStreamRows = ReadInt(row, "complete_profile_stream_rows");
        var exactCommandShapeRows = ReadInt(row, "exact_command_shape_rows");
        var exactCommandShapeExpectedZero = string.Equals(
            Get(row, "exact_command_shape_expected_zero"), "True", StringComparison.OrdinalIgnoreCase);
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-geometry-partition-control", StringComparison.Ordinal) &&
                    adjacencyRows > 0 &&
                    profileRows > 0 &&
                    orderRows > 0 &&
                    joinedObjectRows > 0 &&
                    adjacencyInputStatusValidRows == adjacencyRows &&
                    profileInputStatusValidRows == profileRows &&
                    orderInputStatusValidRows == orderRows &&
                    objectStateExactRows == joinedObjectRows &&
                    geometryGroupStateExactRows == 0 &&
                    profileGeometryPairRows > 0 &&
                    profileStatePairRows > 0 &&
                    profileRowsReconciled == joinedObjectRows &&
                    orderPartitionedStates == adjacencyStates &&
                    orderStates == adjacencyStates &&
                    stateIndexSetMatchRows == joinedObjectRows &&
                    intervalIndexSequenceMatchRows == joinedObjectRows &&
                    intervalBoundaryShapeMatchRows == joinedObjectRows &&
                    completeProfileStreamRows == profileRows &&
                    exactCommandShapeRows == 0 &&
                    exactCommandShapeExpectedZero &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            adjacencyRows,
            profileRows,
            orderRows,
            joinedObjectRows,
            adjacencyInputStatusValidRows,
            profileInputStatusValidRows,
            orderInputStatusValidRows,
            objectStateExactRows,
            geometryGroupStateExactRows,
            profileGeometryPairRows,
            profileStatePairRows,
            profileRowsReconciled,
            orderPartitionedStates,
            adjacencyStates,
            orderStates,
            stateIndexSetMatchRows,
            intervalIndexSequenceMatchRows,
            intervalBoundaryShapeMatchRows,
            completeProfileStreamRows,
            exactCommandShapeRows,
            exactCommandShapeExpectedZero,
            status,
            provenance,
            originalSemantics);
    }

    private static OriginalStateGeometryBindingAmbiguityControlEvidence
        ReadOriginalStateGeometryBindingAmbiguityControlEvidence(string path)
    {
        const string expectedHeader =
            "adjacency_source_report\tprofile_source_report\torder_source_report\tpartition_source_report\tadjacency_rows\tprofile_rows\torder_rows\tpartition_rows\tjoined_object_rows\tmissing_profile_objects\tmissing_order_objects\tmissing_partition_objects\tadjacency_input_status_valid_rows\tprofile_input_status_valid_rows\torder_input_status_valid_rows\tpartition_input_status_valid_rows\tprofile_cardinality_reconciled_rows\torder_cardinality_reconciled_rows\torder_state_index_set_reconciled_rows\tstate_count_greater_than_geometry_rows\tgeometry_group_state_exact_rows\texact_command_shape_rows\tunique_geometry_state_binding_proven_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var adjacencyRows = ReadInt(row, "adjacency_rows");
        var profileRows = ReadInt(row, "profile_rows");
        var orderRows = ReadInt(row, "order_rows");
        var partitionRows = ReadInt(row, "partition_rows");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var missingProfileObjects = ReadInt(row, "missing_profile_objects");
        var missingOrderObjects = ReadInt(row, "missing_order_objects");
        var missingPartitionObjects = ReadInt(row, "missing_partition_objects");
        var adjacencyInputStatusValidRows = ReadInt(row, "adjacency_input_status_valid_rows");
        var profileInputStatusValidRows = ReadInt(row, "profile_input_status_valid_rows");
        var orderInputStatusValidRows = ReadInt(row, "order_input_status_valid_rows");
        var partitionInputStatusValidRows = ReadInt(row, "partition_input_status_valid_rows");
        var profileCardinalityReconciledRows = ReadInt(row, "profile_cardinality_reconciled_rows");
        var orderCardinalityReconciledRows = ReadInt(row, "order_cardinality_reconciled_rows");
        var orderStateIndexSetReconciledRows = ReadInt(row, "order_state_index_set_reconciled_rows");
        var stateCountGreaterThanGeometryRows = ReadInt(row, "state_count_greater_than_geometry_rows");
        var geometryGroupStateExactRows = ReadInt(row, "geometry_group_state_exact_rows");
        var exactCommandShapeRows = ReadInt(row, "exact_command_shape_rows");
        var uniqueGeometryStateBindingProvenRows = ReadInt(row, "unique_geometry_state_binding_proven_rows");
        var validObjectRows = ReadInt(row, "valid_object_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-geometry-binding-ambiguity-control", StringComparison.Ordinal) &&
                    adjacencyRows > 0 &&
                    profileRows > 0 &&
                    orderRows > 0 &&
                    partitionRows > 0 &&
                    joinedObjectRows > 0 &&
                    missingProfileObjects == 0 &&
                    missingOrderObjects == 0 &&
                    missingPartitionObjects == 0 &&
                    adjacencyInputStatusValidRows == adjacencyRows &&
                    profileInputStatusValidRows == profileRows &&
                    orderInputStatusValidRows == orderRows &&
                    partitionInputStatusValidRows == partitionRows &&
                    profileCardinalityReconciledRows == joinedObjectRows &&
                    orderCardinalityReconciledRows == joinedObjectRows &&
                    orderStateIndexSetReconciledRows == joinedObjectRows &&
                    stateCountGreaterThanGeometryRows == joinedObjectRows &&
                    geometryGroupStateExactRows == 0 &&
                    exactCommandShapeRows == 0 &&
                    uniqueGeometryStateBindingProvenRows == 0 &&
                    validObjectRows == joinedObjectRows &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            adjacencyRows,
            profileRows,
            orderRows,
            partitionRows,
            joinedObjectRows,
            missingProfileObjects,
            missingOrderObjects,
            missingPartitionObjects,
            adjacencyInputStatusValidRows,
            profileInputStatusValidRows,
            orderInputStatusValidRows,
            partitionInputStatusValidRows,
            profileCardinalityReconciledRows,
            orderCardinalityReconciledRows,
            orderStateIndexSetReconciledRows,
            stateCountGreaterThanGeometryRows,
            geometryGroupStateExactRows,
            exactCommandShapeRows,
            uniqueGeometryStateBindingProvenRows,
            validObjectRows,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalStateGeometryGlobalAssignmentControlEvidence
        ReadOriginalStateGeometryGlobalAssignmentControlEvidence(string path)
    {
        const string expectedHeader =
            "order_source_report\tpartition_source_report\torder_rows\tpartition_rows\tjoined_object_rows\tmissing_partition_objects\torder_input_status_valid_rows\tpartition_input_status_valid_rows\tnonempty_interval_rows\tambiguous_interval_rows\tobjects_with_multiple_assignment_models\tobjects_with_unique_assignment_model\tobjects_with_one_to_one_unique_model\tunconstrained_assignment_model_sum\tone_to_one_assignment_model_sum\tstate_index_set_reconciled_rows\tunique_state_geometry_assignment_proven_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var orderRows = ReadInt(row, "order_rows");
        var partitionRows = ReadInt(row, "partition_rows");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var missingPartitionObjects = ReadInt(row, "missing_partition_objects");
        var orderInputStatusValidRows = ReadInt(row, "order_input_status_valid_rows");
        var partitionInputStatusValidRows = ReadInt(row, "partition_input_status_valid_rows");
        var nonemptyIntervalRows = ReadInt(row, "nonempty_interval_rows");
        var ambiguousIntervalRows = ReadInt(row, "ambiguous_interval_rows");
        var objectsWithMultipleAssignmentModels = ReadInt(row, "objects_with_multiple_assignment_models");
        var objectsWithUniqueAssignmentModel = ReadInt(row, "objects_with_unique_assignment_model");
        var objectsWithOneToOneUniqueModel = ReadInt(row, "objects_with_one_to_one_unique_model");
        var unconstrainedAssignmentModelSum = ReadInt(row, "unconstrained_assignment_model_sum");
        var oneToOneAssignmentModelSum = ReadInt(row, "one_to_one_assignment_model_sum");
        var stateIndexSetReconciledRows = ReadInt(row, "state_index_set_reconciled_rows");
        var uniqueStateGeometryAssignmentProvenRows = ReadInt(row, "unique_state_geometry_assignment_proven_rows");
        var validObjectRows = ReadInt(row, "valid_object_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-geometry-global-assignment-control", StringComparison.Ordinal) &&
                    orderRows > 0 &&
                    partitionRows > 0 &&
                    joinedObjectRows > 0 &&
                    missingPartitionObjects == 0 &&
                    orderInputStatusValidRows == orderRows &&
                    partitionInputStatusValidRows == partitionRows &&
                    nonemptyIntervalRows > 0 &&
                    ambiguousIntervalRows >= 0 &&
                    objectsWithMultipleAssignmentModels + objectsWithUniqueAssignmentModel == joinedObjectRows &&
                    objectsWithOneToOneUniqueModel >= 0 &&
                    unconstrainedAssignmentModelSum >= joinedObjectRows &&
                    oneToOneAssignmentModelSum >= 0 &&
                    stateIndexSetReconciledRows == joinedObjectRows &&
                    uniqueStateGeometryAssignmentProvenRows == 0 &&
                    validObjectRows == joinedObjectRows &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            orderRows,
            partitionRows,
            joinedObjectRows,
            missingPartitionObjects,
            orderInputStatusValidRows,
            partitionInputStatusValidRows,
            nonemptyIntervalRows,
            ambiguousIntervalRows,
            objectsWithMultipleAssignmentModels,
            objectsWithUniqueAssignmentModel,
            objectsWithOneToOneUniqueModel,
            unconstrainedAssignmentModelSum,
            oneToOneAssignmentModelSum,
            stateIndexSetReconciledRows,
            uniqueStateGeometryAssignmentProvenRows,
            validObjectRows,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalStateGeometryUvMaterialGlobalAssignmentControlEvidence
        ReadOriginalStateGeometryUvMaterialGlobalAssignmentControlEvidence(string path)
    {
        const string expectedHeader =
            "global_source_report\tuv_source_report\tmaterial_source_report\tglobal_rows\tuv_rows\tmaterial_rows\tjoined_object_rows\tmissing_uv_objects\tmissing_material_objects\tglobal_input_status_valid_rows\tuv_candidate_attribute_valid_rows\tuv_candidate_raster_influence_valid_rows\tuv_complete_group_rows\tuv_incomplete_group_rows\tuv_slot1_near_unit_group_rows\tuv_other_slot_near_unit_group_rows\tmaterial_input_status_valid_rows\tmaterial_candidate_asset_valid_rows\tmaterial_state_candidate_source_complete_rows\tobjects_with_multiple_geometry_assignment_models\tobjects_with_single_geometry_assignment_model\tobjects_with_single_candidate_uv_role_domain\tobjects_with_single_candidate_material_domain\tstructural_cross_product_model_sum\tobjects_with_multiple_structural_cross_product_models\tobjects_with_single_structural_cross_product_model\tunique_state_geometry_uv_material_binding_proven_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var globalRows = ReadInt(row, "global_rows");
        var uvRows = ReadInt(row, "uv_rows");
        var materialRows = ReadInt(row, "material_rows");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var missingUvObjects = ReadInt(row, "missing_uv_objects");
        var missingMaterialObjects = ReadInt(row, "missing_material_objects");
        var globalInputStatusValidRows = ReadInt(row, "global_input_status_valid_rows");
        var uvCandidateAttributeValidRows = ReadInt(row, "uv_candidate_attribute_valid_rows");
        var uvCandidateRasterInfluenceValidRows = ReadInt(row, "uv_candidate_raster_influence_valid_rows");
        var uvCompleteGroupRows = ReadInt(row, "uv_complete_group_rows");
        var uvIncompleteGroupRows = ReadInt(row, "uv_incomplete_group_rows");
        var uvSlot1NearUnitGroupRows = ReadInt(row, "uv_slot1_near_unit_group_rows");
        var uvOtherSlotNearUnitGroupRows = ReadInt(row, "uv_other_slot_near_unit_group_rows");
        var materialInputStatusValidRows = ReadInt(row, "material_input_status_valid_rows");
        var materialCandidateAssetValidRows = ReadInt(row, "material_candidate_asset_valid_rows");
        var materialStateCandidateSourceCompleteRows = ReadInt(row, "material_state_candidate_source_complete_rows");
        var objectsWithMultipleGeometryAssignmentModels = ReadInt(row, "objects_with_multiple_geometry_assignment_models");
        var objectsWithSingleGeometryAssignmentModel = ReadInt(row, "objects_with_single_geometry_assignment_model");
        var objectsWithSingleCandidateUvRoleDomain = ReadInt(row, "objects_with_single_candidate_uv_role_domain");
        var objectsWithSingleCandidateMaterialDomain = ReadInt(row, "objects_with_single_candidate_material_domain");
        var structuralCrossProductModelSum = ReadInt(row, "structural_cross_product_model_sum");
        var objectsWithMultipleStructuralCrossProductModels = ReadInt(row, "objects_with_multiple_structural_cross_product_models");
        var objectsWithSingleStructuralCrossProductModel = ReadInt(row, "objects_with_single_structural_cross_product_model");
        var uniqueStateGeometryUvMaterialBindingProvenRows = ReadInt(row, "unique_state_geometry_uv_material_binding_proven_rows");
        var validObjectRows = ReadInt(row, "valid_object_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-geometry-uv-material-global-assignment-control", StringComparison.Ordinal) &&
                    globalRows > 0 &&
                    uvRows > 0 &&
                    materialRows > 0 &&
                    joinedObjectRows > 0 &&
                    missingUvObjects == 0 &&
                    missingMaterialObjects == 0 &&
                    globalInputStatusValidRows == joinedObjectRows &&
                    uvCandidateAttributeValidRows == joinedObjectRows &&
                    uvCandidateRasterInfluenceValidRows == joinedObjectRows &&
                    uvCompleteGroupRows > 0 &&
                    uvIncompleteGroupRows >= 0 &&
                    uvSlot1NearUnitGroupRows <= uvCompleteGroupRows &&
                    uvOtherSlotNearUnitGroupRows >= 0 &&
                    materialInputStatusValidRows == joinedObjectRows &&
                    materialCandidateAssetValidRows == joinedObjectRows &&
                    materialStateCandidateSourceCompleteRows == joinedObjectRows &&
                    objectsWithMultipleGeometryAssignmentModels + objectsWithSingleGeometryAssignmentModel == joinedObjectRows &&
                    objectsWithSingleCandidateUvRoleDomain == joinedObjectRows &&
                    objectsWithSingleCandidateMaterialDomain == joinedObjectRows &&
                    structuralCrossProductModelSum >= joinedObjectRows &&
                    objectsWithMultipleStructuralCrossProductModels + objectsWithSingleStructuralCrossProductModel == joinedObjectRows &&
                    uniqueStateGeometryUvMaterialBindingProvenRows == 0 &&
                    validObjectRows == joinedObjectRows &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            globalRows,
            uvRows,
            materialRows,
            joinedObjectRows,
            missingUvObjects,
            missingMaterialObjects,
            globalInputStatusValidRows,
            uvCandidateAttributeValidRows,
            uvCandidateRasterInfluenceValidRows,
            uvCompleteGroupRows,
            uvIncompleteGroupRows,
            uvSlot1NearUnitGroupRows,
            uvOtherSlotNearUnitGroupRows,
            materialInputStatusValidRows,
            materialCandidateAssetValidRows,
            materialStateCandidateSourceCompleteRows,
            objectsWithMultipleGeometryAssignmentModels,
            objectsWithSingleGeometryAssignmentModel,
            objectsWithSingleCandidateUvRoleDomain,
            objectsWithSingleCandidateMaterialDomain,
            structuralCrossProductModelSum,
            objectsWithMultipleStructuralCrossProductModels,
            objectsWithSingleStructuralCrossProductModel,
            uniqueStateGeometryUvMaterialBindingProvenRows,
            validObjectRows,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalStateGeometryCandidateRenderGlobalAssignmentControlEvidence
        ReadOriginalStateGeometryCandidateRenderGlobalAssignmentControlEvidence(string path)
    {
        const string expectedHeader =
            "global_source_report\truntime_source_report\tcandidate_source_report\tpayload_join_source_report\tglobal_rows\truntime_rows\tcandidate_rows\tpayload_join_rows\tjoined_object_rows\tmissing_runtime_objects\tmissing_candidate_objects\tmissing_payload_join_objects\tglobal_input_status_valid_rows\truntime_input_status_valid_rows\tcandidate_input_status_valid_rows\tpayload_join_input_status_valid_rows\tgeometry_candidate_count_sum\tcandidate_geometry_group_sum\tgeometry_candidate_cardinality_match_rows\tobject_state_exact_rows\trender_group_state_match_rows\tcandidate_source_chain_valid_rows\tpayload_object_join_valid_rows\tobjects_with_multiple_geometry_assignment_models\tobjects_with_single_geometry_assignment_models\tunconstrained_assignment_model_sum\tone_to_one_assignment_model_sum\tunique_state_geometry_candidate_render_binding_proven_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var row = ReadTsvRows(path).FirstOrDefault();
        var globalRows = ReadInt(row, "global_rows");
        var runtimeRows = ReadInt(row, "runtime_rows");
        var candidateRows = ReadInt(row, "candidate_rows");
        var payloadJoinRows = ReadInt(row, "payload_join_rows");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var missingRuntimeObjects = ReadInt(row, "missing_runtime_objects");
        var missingCandidateObjects = ReadInt(row, "missing_candidate_objects");
        var missingPayloadJoinObjects = ReadInt(row, "missing_payload_join_objects");
        var globalInputStatusValidRows = ReadInt(row, "global_input_status_valid_rows");
        var runtimeInputStatusValidRows = ReadInt(row, "runtime_input_status_valid_rows");
        var candidateInputStatusValidRows = ReadInt(row, "candidate_input_status_valid_rows");
        var payloadJoinInputStatusValidRows = ReadInt(row, "payload_join_input_status_valid_rows");
        var geometryCandidateCountSum = ReadInt(row, "geometry_candidate_count_sum");
        var candidateGeometryGroupSum = ReadInt(row, "candidate_geometry_group_sum");
        var geometryCandidateCardinalityMatchRows = ReadInt(row, "geometry_candidate_cardinality_match_rows");
        var objectStateExactRows = ReadInt(row, "object_state_exact_rows");
        var renderGroupStateMatchRows = ReadInt(row, "render_group_state_match_rows");
        var candidateSourceChainValidRows = ReadInt(row, "candidate_source_chain_valid_rows");
        var payloadObjectJoinValidRows = ReadInt(row, "payload_object_join_valid_rows");
        var objectsWithMultipleGeometryAssignmentModels = ReadInt(row, "objects_with_multiple_geometry_assignment_models");
        var objectsWithSingleGeometryAssignmentModels = ReadInt(row, "objects_with_single_geometry_assignment_models");
        var unconstrainedAssignmentModelSum = ReadInt(row, "unconstrained_assignment_model_sum");
        var oneToOneAssignmentModelSum = ReadInt(row, "one_to_one_assignment_model_sum");
        var uniqueStateGeometryCandidateRenderBindingProvenRows = ReadInt(row, "unique_state_geometry_candidate_render_binding_proven_rows");
        var validObjectRows = ReadInt(row, "valid_object_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-geometry-candidate-render-global-assignment-control", StringComparison.Ordinal) &&
                    globalRows > 0 &&
                    runtimeRows > 0 &&
                    candidateRows > 0 &&
                    payloadJoinRows > 0 &&
                    joinedObjectRows > 0 &&
                    missingRuntimeObjects == 0 &&
                    missingCandidateObjects == 0 &&
                    missingPayloadJoinObjects == 0 &&
                    globalInputStatusValidRows == joinedObjectRows &&
                    runtimeInputStatusValidRows == joinedObjectRows &&
                    candidateInputStatusValidRows == joinedObjectRows &&
                    payloadJoinInputStatusValidRows == joinedObjectRows &&
                    geometryCandidateCountSum > 0 &&
                    candidateGeometryGroupSum == geometryCandidateCountSum &&
                    geometryCandidateCardinalityMatchRows == joinedObjectRows &&
                    objectStateExactRows == joinedObjectRows &&
                    renderGroupStateMatchRows == 0 &&
                    candidateSourceChainValidRows == joinedObjectRows &&
                    payloadObjectJoinValidRows == joinedObjectRows &&
                    objectsWithMultipleGeometryAssignmentModels + objectsWithSingleGeometryAssignmentModels == joinedObjectRows &&
                    unconstrainedAssignmentModelSum >= joinedObjectRows &&
                    oneToOneAssignmentModelSum >= 0 &&
                    uniqueStateGeometryCandidateRenderBindingProvenRows == 0 &&
                    validObjectRows == joinedObjectRows &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            globalRows,
            runtimeRows,
            candidateRows,
            payloadJoinRows,
            joinedObjectRows,
            missingRuntimeObjects,
            missingCandidateObjects,
            missingPayloadJoinObjects,
            globalInputStatusValidRows,
            runtimeInputStatusValidRows,
            candidateInputStatusValidRows,
            payloadJoinInputStatusValidRows,
            geometryCandidateCountSum,
            candidateGeometryGroupSum,
            geometryCandidateCardinalityMatchRows,
            objectStateExactRows,
            renderGroupStateMatchRows,
            candidateSourceChainValidRows,
            payloadObjectJoinValidRows,
            objectsWithMultipleGeometryAssignmentModels,
            objectsWithSingleGeometryAssignmentModels,
            unconstrainedAssignmentModelSum,
            oneToOneAssignmentModelSum,
            uniqueStateGeometryCandidateRenderBindingProvenRows,
            validObjectRows,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence
        ReadOriginalStateGeometryPayloadIdentityGlobalAssignmentControlEvidence(string path)
    {
        const string expectedHeader =
            "global_source_report\tpayload_identity_source_report\tglobal_rows\tpayload_identity_rows\tjoined_object_rows\tmissing_payload_identity_objects\tglobal_input_status_valid_rows\tpayload_identity_input_status_valid_rows\tgeometry_candidate_count_sum\tpayload_identity_geometry_row_sum\tpayload_identity_complete_geometry_sum\tpayload_identity_raw_hash_row_sum\tpayload_identity_cardinality_match_rows\tpayload_identity_reduced_state_domain_rows\tobjects_with_multiple_assignment_models\tobjects_with_single_assignment_models\tassignment_model_sum_before_payload_identity\tassignment_model_sum_after_payload_identity\tone_to_one_model_sum_before_payload_identity\tone_to_one_model_sum_after_payload_identity\tunique_state_geometry_payload_binding_proven_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var row = ReadTsvRows(path).FirstOrDefault();
        var globalRows = ReadInt(row, "global_rows");
        var payloadIdentityRows = ReadInt(row, "payload_identity_rows");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var missingPayloadIdentityObjects = ReadInt(row, "missing_payload_identity_objects");
        var globalInputStatusValidRows = ReadInt(row, "global_input_status_valid_rows");
        var payloadIdentityInputStatusValidRows = ReadInt(row, "payload_identity_input_status_valid_rows");
        var geometryCandidateCountSum = ReadInt(row, "geometry_candidate_count_sum");
        var payloadIdentityGeometryRowSum = ReadInt(row, "payload_identity_geometry_row_sum");
        var payloadIdentityCompleteGeometrySum = ReadInt(row, "payload_identity_complete_geometry_sum");
        var payloadIdentityRawHashRowSum = ReadInt(row, "payload_identity_raw_hash_row_sum");
        var payloadIdentityCardinalityMatchRows = ReadInt(row, "payload_identity_cardinality_match_rows");
        var payloadIdentityReducedStateDomainRows = ReadInt(row, "payload_identity_reduced_state_domain_rows");
        var objectsWithMultipleAssignmentModels = ReadInt(row, "objects_with_multiple_assignment_models");
        var objectsWithSingleAssignmentModels = ReadInt(row, "objects_with_single_assignment_models");
        var assignmentModelSumBeforePayloadIdentity = ReadInt(row, "assignment_model_sum_before_payload_identity");
        var assignmentModelSumAfterPayloadIdentity = ReadInt(row, "assignment_model_sum_after_payload_identity");
        var oneToOneModelSumBeforePayloadIdentity = ReadInt(row, "one_to_one_model_sum_before_payload_identity");
        var oneToOneModelSumAfterPayloadIdentity = ReadInt(row, "one_to_one_model_sum_after_payload_identity");
        var uniqueStateGeometryPayloadBindingProvenRows = ReadInt(row, "unique_state_geometry_payload_binding_proven_rows");
        var validObjectRows = ReadInt(row, "valid_object_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-geometry-payload-identity-global-assignment-control", StringComparison.Ordinal) &&
                    globalRows > 0 &&
                    payloadIdentityRows > 0 &&
                    joinedObjectRows > 0 &&
                    missingPayloadIdentityObjects == 0 &&
                    globalInputStatusValidRows == joinedObjectRows &&
                    payloadIdentityInputStatusValidRows == joinedObjectRows &&
                    geometryCandidateCountSum > 0 &&
                    payloadIdentityGeometryRowSum == geometryCandidateCountSum &&
                    payloadIdentityCompleteGeometrySum == payloadIdentityGeometryRowSum &&
                    payloadIdentityRawHashRowSum > 0 &&
                    payloadIdentityCardinalityMatchRows == joinedObjectRows &&
                    payloadIdentityReducedStateDomainRows == 0 &&
                    objectsWithMultipleAssignmentModels + objectsWithSingleAssignmentModels == joinedObjectRows &&
                    assignmentModelSumAfterPayloadIdentity == assignmentModelSumBeforePayloadIdentity &&
                    oneToOneModelSumAfterPayloadIdentity == oneToOneModelSumBeforePayloadIdentity &&
                    uniqueStateGeometryPayloadBindingProvenRows == 0 &&
                    validObjectRows == joinedObjectRows &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            globalRows,
            payloadIdentityRows,
            joinedObjectRows,
            missingPayloadIdentityObjects,
            globalInputStatusValidRows,
            payloadIdentityInputStatusValidRows,
            geometryCandidateCountSum,
            payloadIdentityGeometryRowSum,
            payloadIdentityCompleteGeometrySum,
            payloadIdentityRawHashRowSum,
            payloadIdentityCardinalityMatchRows,
            payloadIdentityReducedStateDomainRows,
            objectsWithMultipleAssignmentModels,
            objectsWithSingleAssignmentModels,
            assignmentModelSumBeforePayloadIdentity,
            assignmentModelSumAfterPayloadIdentity,
            oneToOneModelSumBeforePayloadIdentity,
            oneToOneModelSumAfterPayloadIdentity,
            uniqueStateGeometryPayloadBindingProvenRows,
            validObjectRows,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalStateGeometryDrawClassificationGlobalAssignmentControlEvidence
        ReadOriginalStateGeometryDrawClassificationGlobalAssignmentControlEvidence(string path)
    {
        const string expectedHeader =
            "global_source_report\tdraw_classification_source_report\tpayload_identity_source_report\tglobal_rows\tdraw_classification_rows\tpayload_identity_rows\tjoined_object_rows\tmissing_draw_classification_objects\tmissing_payload_identity_objects\tglobal_input_status_valid_rows\tdraw_classification_input_status_valid_rows\tpayload_identity_input_status_valid_rows\tgeometry_candidate_count_sum\tdraw_classification_geometry_marker_sum\tpayload_identity_geometry_row_sum\tclassification_match_rows\tgeometry_draw_payload_identity_join_rows\tstate_marker_rows\tstate_rows_without_payload_identity\tobjects_with_state_over_geometry_rows\tassignment_model_sum_before_draw_classification\tassignment_model_sum_after_draw_classification\tone_to_one_model_sum_before_draw_classification\tone_to_one_model_sum_after_draw_classification\tdraw_classification_reduced_state_domain_rows\tunique_state_geometry_draw_binding_proven_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var row = ReadTsvRows(path).FirstOrDefault();
        var globalRows = ReadInt(row, "global_rows");
        var drawClassificationRows = ReadInt(row, "draw_classification_rows");
        var payloadIdentityRows = ReadInt(row, "payload_identity_rows");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var missingDrawClassificationObjects = ReadInt(row, "missing_draw_classification_objects");
        var missingPayloadIdentityObjects = ReadInt(row, "missing_payload_identity_objects");
        var globalInputStatusValidRows = ReadInt(row, "global_input_status_valid_rows");
        var drawClassificationInputStatusValidRows = ReadInt(row, "draw_classification_input_status_valid_rows");
        var payloadIdentityInputStatusValidRows = ReadInt(row, "payload_identity_input_status_valid_rows");
        var geometryCandidateCountSum = ReadInt(row, "geometry_candidate_count_sum");
        var drawClassificationGeometryMarkerSum = ReadInt(row, "draw_classification_geometry_marker_sum");
        var payloadIdentityGeometryRowSum = ReadInt(row, "payload_identity_geometry_row_sum");
        var classificationMatchRows = ReadInt(row, "classification_match_rows");
        var geometryDrawPayloadIdentityJoinRows = ReadInt(row, "geometry_draw_payload_identity_join_rows");
        var stateMarkerRows = ReadInt(row, "state_marker_rows");
        var stateRowsWithoutPayloadIdentity = ReadInt(row, "state_rows_without_payload_identity");
        var objectsWithStateOverGeometryRows = ReadInt(row, "objects_with_state_over_geometry_rows");
        var assignmentModelSumBeforeDrawClassification = ReadInt(row, "assignment_model_sum_before_draw_classification");
        var assignmentModelSumAfterDrawClassification = ReadInt(row, "assignment_model_sum_after_draw_classification");
        var oneToOneModelSumBeforeDrawClassification = ReadInt(row, "one_to_one_model_sum_before_draw_classification");
        var oneToOneModelSumAfterDrawClassification = ReadInt(row, "one_to_one_model_sum_after_draw_classification");
        var drawClassificationReducedStateDomainRows = ReadInt(row, "draw_classification_reduced_state_domain_rows");
        var uniqueStateGeometryDrawBindingProvenRows = ReadInt(row, "unique_state_geometry_draw_binding_proven_rows");
        var validObjectRows = ReadInt(row, "valid_object_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-geometry-draw-classification-global-assignment-control", StringComparison.Ordinal) &&
                    globalRows > 0 &&
                    drawClassificationRows > 0 &&
                    payloadIdentityRows > 0 &&
                    joinedObjectRows > 0 &&
                    missingDrawClassificationObjects == 0 &&
                    missingPayloadIdentityObjects == 0 &&
                    globalInputStatusValidRows == joinedObjectRows &&
                    drawClassificationInputStatusValidRows > 0 &&
                    payloadIdentityInputStatusValidRows > 0 &&
                    geometryCandidateCountSum == drawClassificationGeometryMarkerSum &&
                    payloadIdentityGeometryRowSum == drawClassificationGeometryMarkerSum &&
                    classificationMatchRows == drawClassificationRows &&
                    geometryDrawPayloadIdentityJoinRows == drawClassificationGeometryMarkerSum &&
                    stateMarkerRows == stateRowsWithoutPayloadIdentity &&
                    objectsWithStateOverGeometryRows == joinedObjectRows &&
                    assignmentModelSumAfterDrawClassification == assignmentModelSumBeforeDrawClassification &&
                    oneToOneModelSumAfterDrawClassification == oneToOneModelSumBeforeDrawClassification &&
                    drawClassificationReducedStateDomainRows == 0 &&
                    uniqueStateGeometryDrawBindingProvenRows == 0 &&
                    validObjectRows == joinedObjectRows &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            globalRows,
            drawClassificationRows,
            payloadIdentityRows,
            joinedObjectRows,
            missingDrawClassificationObjects,
            missingPayloadIdentityObjects,
            globalInputStatusValidRows,
            drawClassificationInputStatusValidRows,
            payloadIdentityInputStatusValidRows,
            geometryCandidateCountSum,
            drawClassificationGeometryMarkerSum,
            payloadIdentityGeometryRowSum,
            classificationMatchRows,
            geometryDrawPayloadIdentityJoinRows,
            stateMarkerRows,
            stateRowsWithoutPayloadIdentity,
            objectsWithStateOverGeometryRows,
            assignmentModelSumBeforeDrawClassification,
            assignmentModelSumAfterDrawClassification,
            oneToOneModelSumBeforeDrawClassification,
            oneToOneModelSumAfterDrawClassification,
            drawClassificationReducedStateDomainRows,
            uniqueStateGeometryDrawBindingProvenRows,
            validObjectRows,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalStateUidAmbiguityControlEvidence
        ReadOriginalStateUidAmbiguityControlEvidence(string path)
    {
        const string expectedHeader =
            "candidate_source_report\trender_source_report\tcandidate_object_rows\trender_scoped_rows\tjoined_object_rows\tmissing_candidate_objects\tmissing_render_objects\tgeometry_rows\tstate_rows\tgeometry_rows_with_valid_uid\tstate_rows_with_valid_uid\tshared_uid_count\tgeometry_rows_same_uid_state_candidates\tgeometry_rows_zero_same_uid_state_candidates\tgeometry_rows_one_same_uid_state_candidate\tgeometry_rows_multiple_same_uid_state_candidates\tstate_rows_in_shared_uid_domains\tobjects_with_uid_ambiguity\tunique_geometry_state_binding_proven_rows\tcandidate_input_status_valid_rows\trender_input_status_valid_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var candidateObjectRows = ReadInt(row, "candidate_object_rows");
        var renderScopedRows = ReadInt(row, "render_scoped_rows");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var missingCandidateObjects = ReadInt(row, "missing_candidate_objects");
        var missingRenderObjects = ReadInt(row, "missing_render_objects");
        var geometryRows = ReadInt(row, "geometry_rows");
        var stateRows = ReadInt(row, "state_rows");
        var geometryRowsWithValidUid = ReadInt(row, "geometry_rows_with_valid_uid");
        var stateRowsWithValidUid = ReadInt(row, "state_rows_with_valid_uid");
        var sharedUidCount = ReadInt(row, "shared_uid_count");
        var geometryRowsSameUidStateCandidates = ReadInt(row, "geometry_rows_same_uid_state_candidates");
        var geometryRowsZeroSameUidStateCandidates = ReadInt(row, "geometry_rows_zero_same_uid_state_candidates");
        var geometryRowsOneSameUidStateCandidate = ReadInt(row, "geometry_rows_one_same_uid_state_candidate");
        var geometryRowsMultipleSameUidStateCandidates = ReadInt(row, "geometry_rows_multiple_same_uid_state_candidates");
        var stateRowsInSharedUidDomains = ReadInt(row, "state_rows_in_shared_uid_domains");
        var objectsWithUidAmbiguity = ReadInt(row, "objects_with_uid_ambiguity");
        var uniqueGeometryStateBindingProvenRows = ReadInt(row, "unique_geometry_state_binding_proven_rows");
        var candidateInputStatusValidRows = ReadInt(row, "candidate_input_status_valid_rows");
        var renderInputStatusValidRows = ReadInt(row, "render_input_status_valid_rows");
        var validObjectRows = ReadInt(row, "valid_object_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-runtime-uid-ambiguity-control", StringComparison.Ordinal) &&
                    candidateObjectRows == 24 &&
                    renderScopedRows == 218 &&
                    joinedObjectRows == 24 &&
                    missingCandidateObjects == 0 &&
                    missingRenderObjects == 0 &&
                    geometryRows == 38 &&
                    stateRows == 180 &&
                    geometryRowsWithValidUid == 38 &&
                    stateRowsWithValidUid == 180 &&
                    sharedUidCount == 24 &&
                    geometryRowsSameUidStateCandidates == 38 &&
                    geometryRowsZeroSameUidStateCandidates == 0 &&
                    geometryRowsOneSameUidStateCandidate == 0 &&
                    geometryRowsMultipleSameUidStateCandidates == 38 &&
                    stateRowsInSharedUidDomains == 180 &&
                    objectsWithUidAmbiguity == 24 &&
                    uniqueGeometryStateBindingProvenRows == 0 &&
                    candidateInputStatusValidRows == 24 &&
                    renderInputStatusValidRows == 24 &&
                    validObjectRows == 24 &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            candidateObjectRows,
            renderScopedRows,
            joinedObjectRows,
            missingCandidateObjects,
            missingRenderObjects,
            geometryRows,
            stateRows,
            geometryRowsWithValidUid,
            stateRowsWithValidUid,
            sharedUidCount,
            geometryRowsSameUidStateCandidates,
            geometryRowsZeroSameUidStateCandidates,
            geometryRowsOneSameUidStateCandidate,
            geometryRowsMultipleSameUidStateCandidates,
            stateRowsInSharedUidDomains,
            objectsWithUidAmbiguity,
            uniqueGeometryStateBindingProvenRows,
            candidateInputStatusValidRows,
            renderInputStatusValidRows,
            validObjectRows,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalStateUidCommandHashAmbiguityControlEvidence
        ReadOriginalStateUidCommandHashAmbiguityControlEvidence(string path)
    {
        const string expectedHeader =
            "candidate_source_report\trender_source_report\tcandidate_object_rows\trender_scoped_rows\tjoined_object_rows\tmissing_candidate_objects\tmissing_render_objects\tgeometry_rows\tstate_rows\tgeometry_rows_with_valid_uid\tstate_rows_with_valid_uid\tshared_uid_count\tgeometry_rows_same_uid_state_candidates\tgeometry_rows_zero_same_uid_state_candidates\tgeometry_rows_one_same_uid_state_candidate\tgeometry_rows_multiple_same_uid_state_candidates\tgeometry_rows_with_uid_and_command_hash_candidates\tgeometry_rows_with_zero_uid_and_command_hash_candidates\tgeometry_rows_with_one_uid_and_command_hash_candidate\tgeometry_rows_with_multiple_uid_and_command_hash_candidates\tstate_rows_in_shared_uid_domains\tobjects_with_uid_ambiguity\tobjects_with_uid_and_command_hash_ambiguity\tunique_geometry_state_binding_proven_rows\tunique_geometry_state_binding_with_command_hash_proven_rows\tcandidate_input_status_valid_rows\trender_input_status_valid_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var candidateObjectRows = ReadInt(row, "candidate_object_rows");
        var renderScopedRows = ReadInt(row, "render_scoped_rows");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var missingCandidateObjects = ReadInt(row, "missing_candidate_objects");
        var missingRenderObjects = ReadInt(row, "missing_render_objects");
        var geometryRows = ReadInt(row, "geometry_rows");
        var stateRows = ReadInt(row, "state_rows");
        var geometryRowsWithValidUid = ReadInt(row, "geometry_rows_with_valid_uid");
        var stateRowsWithValidUid = ReadInt(row, "state_rows_with_valid_uid");
        var sharedUidCount = ReadInt(row, "shared_uid_count");
        var geometryRowsSameUidStateCandidates = ReadInt(row, "geometry_rows_same_uid_state_candidates");
        var geometryRowsZeroSameUidStateCandidates = ReadInt(row, "geometry_rows_zero_same_uid_state_candidates");
        var geometryRowsOneSameUidStateCandidate = ReadInt(row, "geometry_rows_one_same_uid_state_candidate");
        var geometryRowsMultipleSameUidStateCandidates = ReadInt(row, "geometry_rows_multiple_same_uid_state_candidates");
        var geometryRowsWithUidAndCommandHashCandidates = ReadInt(row, "geometry_rows_with_uid_and_command_hash_candidates");
        var geometryRowsWithZeroUidAndCommandHashCandidates = ReadInt(row, "geometry_rows_with_zero_uid_and_command_hash_candidates");
        var geometryRowsWithOneUidAndCommandHashCandidate = ReadInt(row, "geometry_rows_with_one_uid_and_command_hash_candidate");
        var geometryRowsWithMultipleUidAndCommandHashCandidates = ReadInt(row, "geometry_rows_with_multiple_uid_and_command_hash_candidates");
        var stateRowsInSharedUidDomains = ReadInt(row, "state_rows_in_shared_uid_domains");
        var objectsWithUidAmbiguity = ReadInt(row, "objects_with_uid_ambiguity");
        var objectsWithUidAndCommandHashAmbiguity = ReadInt(row, "objects_with_uid_and_command_hash_ambiguity");
        var uniqueGeometryStateBindingProvenRows = ReadInt(row, "unique_geometry_state_binding_proven_rows");
        var uniqueGeometryStateBindingWithCommandHashProvenRows = ReadInt(row, "unique_geometry_state_binding_with_command_hash_proven_rows");
        var candidateInputStatusValidRows = ReadInt(row, "candidate_input_status_valid_rows");
        var renderInputStatusValidRows = ReadInt(row, "render_input_status_valid_rows");
        var validObjectRows = ReadInt(row, "valid_object_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-uid-command-hash-ambiguity-control", StringComparison.Ordinal) &&
                    candidateObjectRows == 24 &&
                    renderScopedRows == 218 &&
                    joinedObjectRows == 24 &&
                    missingCandidateObjects == 0 &&
                    missingRenderObjects == 0 &&
                    geometryRows == 38 &&
                    stateRows == 180 &&
                    geometryRowsWithValidUid == 38 &&
                    stateRowsWithValidUid == 180 &&
                    sharedUidCount == 24 &&
                    geometryRowsSameUidStateCandidates == 38 &&
                    geometryRowsZeroSameUidStateCandidates == 0 &&
                    geometryRowsOneSameUidStateCandidate == 0 &&
                    geometryRowsMultipleSameUidStateCandidates == 38 &&
                    geometryRowsWithUidAndCommandHashCandidates == 0 &&
                    geometryRowsWithZeroUidAndCommandHashCandidates == 38 &&
                    geometryRowsWithOneUidAndCommandHashCandidate == 0 &&
                    geometryRowsWithMultipleUidAndCommandHashCandidates == 0 &&
                    stateRowsInSharedUidDomains == 180 &&
                    objectsWithUidAmbiguity == 24 &&
                    objectsWithUidAndCommandHashAmbiguity == 0 &&
                    uniqueGeometryStateBindingProvenRows == 0 &&
                    uniqueGeometryStateBindingWithCommandHashProvenRows == 0 &&
                    candidateInputStatusValidRows == 218 &&
                    renderInputStatusValidRows == 218 &&
                    validObjectRows == 24 &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            candidateObjectRows,
            renderScopedRows,
            joinedObjectRows,
            missingCandidateObjects,
            missingRenderObjects,
            geometryRows,
            stateRows,
            geometryRowsWithValidUid,
            stateRowsWithValidUid,
            sharedUidCount,
            geometryRowsSameUidStateCandidates,
            geometryRowsZeroSameUidStateCandidates,
            geometryRowsOneSameUidStateCandidate,
            geometryRowsMultipleSameUidStateCandidates,
            geometryRowsWithUidAndCommandHashCandidates,
            geometryRowsWithZeroUidAndCommandHashCandidates,
            geometryRowsWithOneUidAndCommandHashCandidate,
            geometryRowsWithMultipleUidAndCommandHashCandidates,
            stateRowsInSharedUidDomains,
            objectsWithUidAmbiguity,
            objectsWithUidAndCommandHashAmbiguity,
            uniqueGeometryStateBindingProvenRows,
            uniqueGeometryStateBindingWithCommandHashProvenRows,
            candidateInputStatusValidRows,
            renderInputStatusValidRows,
            validObjectRows,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalStateProfileDiscriminationControlEvidence
        ReadOriginalStateProfileDiscriminationControlEvidence(string path)
    {
        const string expectedHeader =
            "profile_source_report\tadjacency_source_report\tprofile_rows\tprofile_objects\tgeometry_candidate_rows\tstate_primitive_rows\tdistinct_object_state_profile_signatures\tprofile_tie_count_match_rows\tstate_primitives_with_multiple_geometry_candidates\tstate_primitives_with_single_geometry_candidate\tstate_primitives_with_zero_geometry_candidates\tstate_signature_groups_with_multiple_geometry_candidates\texact_command_shape_rows\tunique_state_to_geometry_binding_proven_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var profileRows = ReadInt(row, "profile_rows");
        var profileObjects = ReadInt(row, "profile_objects");
        var geometryCandidateRows = ReadInt(row, "geometry_candidate_rows");
        var statePrimitiveRows = ReadInt(row, "state_primitive_rows");
        var distinctObjectStateProfileSignatures = ReadInt(row, "distinct_object_state_profile_signatures");
        var profileTieCountMatchRows = ReadInt(row, "profile_tie_count_match_rows");
        var statePrimitivesWithMultipleGeometryCandidates = ReadInt(row, "state_primitives_with_multiple_geometry_candidates");
        var statePrimitivesWithSingleGeometryCandidate = ReadInt(row, "state_primitives_with_single_geometry_candidate");
        var statePrimitivesWithZeroGeometryCandidates = ReadInt(row, "state_primitives_with_zero_geometry_candidates");
        var stateSignatureGroupsWithMultipleGeometryCandidates = ReadInt(row, "state_signature_groups_with_multiple_geometry_candidates");
        var exactCommandShapeRows = ReadInt(row, "exact_command_shape_rows");
        var uniqueStateToGeometryBindingProvenRows = ReadInt(row, "unique_state_to_geometry_binding_proven_rows");
        var validObjectRows = ReadInt(row, "valid_object_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-profile-discrimination-control", StringComparison.Ordinal) &&
                    profileRows == 269 &&
                    profileObjects == 24 &&
                    geometryCandidateRows == 38 &&
                    statePrimitiveRows == 180 &&
                    distinctObjectStateProfileSignatures == 27 &&
                    profileTieCountMatchRows == 269 &&
                    statePrimitivesWithMultipleGeometryCandidates == 89 &&
                    statePrimitivesWithSingleGeometryCandidate == 91 &&
                    statePrimitivesWithZeroGeometryCandidates == 0 &&
                    stateSignatureGroupsWithMultipleGeometryCandidates == 15 &&
                    exactCommandShapeRows == 0 &&
                    uniqueStateToGeometryBindingProvenRows == 0 &&
                    validObjectRows == 24 &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            profileRows,
            profileObjects,
            geometryCandidateRows,
            statePrimitiveRows,
            distinctObjectStateProfileSignatures,
            profileTieCountMatchRows,
            statePrimitivesWithMultipleGeometryCandidates,
            statePrimitivesWithSingleGeometryCandidate,
            statePrimitivesWithZeroGeometryCandidates,
            stateSignatureGroupsWithMultipleGeometryCandidates,
            exactCommandShapeRows,
            uniqueStateToGeometryBindingProvenRows,
            validObjectRows,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalStateSlot1CandidateUvAmbiguityEvidence
        ReadOriginalStateSlot1CandidateUvAmbiguityEvidence(string path)
    {
        const string expectedHeader =
            "state_source_report\trole_source_report\tattribute_source_report\tinfluence_source_report\tprofile_source_report\tstate_rows\trole_rows\tjoined_primitive_groups\tcandidate_joined_primitive_groups\tmissing_state_keys\tmissing_role_keys\tduplicate_state_keys\tduplicate_role_keys\tcomplete_five_slot_groups\tincomplete_five_slot_groups\tshapename_state_groups\tcomplete_shapename_state_groups\tcomplete_shapename_slot1_near_unit_groups\tcomplete_shapename_other_slot_near_unit_groups\tcomplete_groups_slot1_near_unit_groups\tcomplete_groups_other_slot_near_unit_groups\tcandidate_object_rows\tcandidate_attribute_texture_valid_rows\tcandidate_raster_influence_valid_rows\tunique_state_to_geometry_binding_proven_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var stateRows = ReadInt(row, "state_rows");
        var roleRows = ReadInt(row, "role_rows");
        var joinedPrimitiveGroups = ReadInt(row, "joined_primitive_groups");
        var candidateJoinedPrimitiveGroups = ReadInt(row, "candidate_joined_primitive_groups");
        var missingStateKeys = ReadInt(row, "missing_state_keys");
        var missingRoleKeys = ReadInt(row, "missing_role_keys");
        var duplicateStateKeys = ReadInt(row, "duplicate_state_keys");
        var duplicateRoleKeys = ReadInt(row, "duplicate_role_keys");
        var completeFiveSlotGroups = ReadInt(row, "complete_five_slot_groups");
        var incompleteFiveSlotGroups = ReadInt(row, "incomplete_five_slot_groups");
        var shapenameStateGroups = ReadInt(row, "shapename_state_groups");
        var completeShapenameStateGroups = ReadInt(row, "complete_shapename_state_groups");
        var completeShapenameSlot1NearUnitGroups = ReadInt(row, "complete_shapename_slot1_near_unit_groups");
        var completeShapenameOtherSlotNearUnitGroups = ReadInt(row, "complete_shapename_other_slot_near_unit_groups");
        var completeGroupsSlot1NearUnitGroups = ReadInt(row, "complete_groups_slot1_near_unit_groups");
        var completeGroupsOtherSlotNearUnitGroups = ReadInt(row, "complete_groups_other_slot_near_unit_groups");
        var candidateObjectRows = ReadInt(row, "candidate_object_rows");
        var candidateAttributeTextureValidRows = ReadInt(row, "candidate_attribute_texture_valid_rows");
        var candidateRasterInfluenceValidRows = ReadInt(row, "candidate_raster_influence_valid_rows");
        var uniqueStateToGeometryBindingProvenRows = ReadInt(row, "unique_state_to_geometry_binding_proven_rows");
        var validObjectRows = ReadInt(row, "valid_object_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-slot1-candidate-uv-ambiguity-control", StringComparison.Ordinal) &&
                    stateRows == 2364 &&
                    roleRows == 11820 &&
                    joinedPrimitiveGroups == 2364 &&
                    candidateJoinedPrimitiveGroups == 218 &&
                    missingStateKeys == 0 &&
                    missingRoleKeys == 0 &&
                    duplicateStateKeys == 0 &&
                    duplicateRoleKeys == 0 &&
                    completeFiveSlotGroups == 216 &&
                    incompleteFiveSlotGroups == 2 &&
                    shapenameStateGroups == 180 &&
                    completeShapenameStateGroups == 178 &&
                    completeShapenameSlot1NearUnitGroups == 178 &&
                    completeShapenameOtherSlotNearUnitGroups == 0 &&
                    completeGroupsSlot1NearUnitGroups == 216 &&
                    completeGroupsOtherSlotNearUnitGroups == 0 &&
                    candidateObjectRows == 24 &&
                    candidateAttributeTextureValidRows == 24 &&
                    candidateRasterInfluenceValidRows == 24 &&
                    uniqueStateToGeometryBindingProvenRows == 0 &&
                    validObjectRows == 24 &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            stateRows,
            roleRows,
            joinedPrimitiveGroups,
            candidateJoinedPrimitiveGroups,
            missingStateKeys,
            missingRoleKeys,
            duplicateStateKeys,
            duplicateRoleKeys,
            completeFiveSlotGroups,
            incompleteFiveSlotGroups,
            shapenameStateGroups,
            completeShapenameStateGroups,
            completeShapenameSlot1NearUnitGroups,
            completeShapenameOtherSlotNearUnitGroups,
            completeGroupsSlot1NearUnitGroups,
            completeGroupsOtherSlotNearUnitGroups,
            candidateObjectRows,
            candidateAttributeTextureValidRows,
            candidateRasterInfluenceValidRows,
            uniqueStateToGeometryBindingProvenRows,
            validObjectRows,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static AnimationMorphPoseBankAmbiguityControlEvidence
        ReadAnimationMorphPoseBankAmbiguityControlEvidence(string path)
    {
        const string expectedHeader =
            "pose_source_report\tanimation_source_report\tmorph_source_report\tpose_rows\tdistinct_pose_archives\tdistinct_pose_symbols\tdistinct_normalized_names\tanimation_bank_rows\tdistinct_animation_banks\tanimation_bank_valid_rows\tanimation_bank_symbol_metadata_rows\tmorph_layout_rows\tmorph_parse_valid_rows\tmorph_containing_pairs\tmorph_exact_gap_set_layouts\tmorph_exact_gap_set_pairs\tmorph_exact_gap_histogram_layouts\tmorph_exact_gap_histogram_pairs\tpose_name_match_rows\tpose_symbol_metadata_match_rows\tunique_pose_animation_binding_proven_rows\tvalid_pose_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var poseRows = ReadInt(row, "pose_rows");
        var distinctPoseArchives = ReadInt(row, "distinct_pose_archives");
        var distinctPoseSymbols = ReadInt(row, "distinct_pose_symbols");
        var distinctNormalizedNames = ReadInt(row, "distinct_normalized_names");
        var animationBankRows = ReadInt(row, "animation_bank_rows");
        var distinctAnimationBanks = ReadInt(row, "distinct_animation_banks");
        var animationBankValidRows = ReadInt(row, "animation_bank_valid_rows");
        var animationBankSymbolMetadataRows = ReadInt(row, "animation_bank_symbol_metadata_rows");
        var morphLayoutRows = ReadInt(row, "morph_layout_rows");
        var morphParseValidRows = ReadInt(row, "morph_parse_valid_rows");
        var morphContainingPairs = ReadInt(row, "morph_containing_pairs");
        var morphExactGapSetLayouts = ReadInt(row, "morph_exact_gap_set_layouts");
        var morphExactGapSetPairs = ReadInt(row, "morph_exact_gap_set_pairs");
        var morphExactGapHistogramLayouts = ReadInt(row, "morph_exact_gap_histogram_layouts");
        var morphExactGapHistogramPairs = ReadInt(row, "morph_exact_gap_histogram_pairs");
        var poseNameMatchRows = ReadInt(row, "pose_name_match_rows");
        var poseSymbolMetadataMatchRows = ReadInt(row, "pose_symbol_metadata_match_rows");
        var uniquePoseAnimationBindingProvenRows = ReadInt(row, "unique_pose_animation_binding_proven_rows");
        var validPoseRows = ReadInt(row, "valid_pose_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-animation-pose-bank-ambiguity-control", StringComparison.Ordinal) &&
                    poseRows > 0 &&
                    distinctPoseArchives == poseRows &&
                    distinctPoseSymbols == 1 &&
                    distinctNormalizedNames == 1 &&
                    animationBankRows > 0 &&
                    distinctAnimationBanks == animationBankRows &&
                    animationBankValidRows == animationBankRows &&
                    animationBankSymbolMetadataRows == animationBankRows &&
                    morphLayoutRows > 0 &&
                    morphParseValidRows == morphLayoutRows &&
                    morphContainingPairs > 0 &&
                    morphExactGapSetLayouts == 0 &&
                    morphExactGapSetPairs == 0 &&
                    morphExactGapHistogramLayouts == 0 &&
                    morphExactGapHistogramPairs == 0 &&
                    poseNameMatchRows == 0 &&
                    poseSymbolMetadataMatchRows == 0 &&
                    uniquePoseAnimationBindingProvenRows == 0 &&
                    validPoseRows == poseRows &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            poseRows,
            distinctPoseArchives,
            distinctPoseSymbols,
            distinctNormalizedNames,
            animationBankRows,
            distinctAnimationBanks,
            animationBankValidRows,
            animationBankSymbolMetadataRows,
            morphLayoutRows,
            morphParseValidRows,
            morphContainingPairs,
            morphExactGapSetLayouts,
            morphExactGapSetPairs,
            morphExactGapHistogramLayouts,
            morphExactGapHistogramPairs,
            poseNameMatchRows,
            poseSymbolMetadataMatchRows,
            uniquePoseAnimationBindingProvenRows,
            validPoseRows,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static AnimationCatalogPoseTimingGlobalAssignmentControlEvidence
        ReadAnimationCatalogPoseTimingGlobalAssignmentControlEvidence(string path)
    {
        const string expectedHeader =
            "timestamp_source_report\tanchor_source_report\tpose_source_report\tmorph_source_report\tcatalog_bank_count\tcatalog_rows\tcatalog_timestamp_valid_rows\tanchor_bank_rows\tanchor_complete_bank_rows\tanchor_count\tpose_rows\tdistinct_pose_archives\tdistinct_pose_symbols\tdistinct_normalized_names\tmorph_layout_rows\tmorph_parse_valid_rows\tmorph_containing_layouts\tmorph_containing_pairs\tmorph_unique_containment_layouts\tmorph_ambiguous_containment_layouts\tmorph_exact_gap_set_pairs\tpose_rows_without_catalog_join_key\tpose_rows_without_morph_join_key\tpose_bank_domain_size_before\tpose_bank_domain_size_after\tglobal_assignment_models_before\tglobal_assignment_models_after\tpose_bank_domain_reduced_rows\tunique_pose_animation_binding_proven_rows\tvalid_pose_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var catalogBankCount = ReadInt(row, "catalog_bank_count");
        var catalogRows = ReadInt(row, "catalog_rows");
        var catalogTimestampValidRows = ReadInt(row, "catalog_timestamp_valid_rows");
        var anchorBankRows = ReadInt(row, "anchor_bank_rows");
        var anchorCompleteBankRows = ReadInt(row, "anchor_complete_bank_rows");
        var anchorCount = ReadInt(row, "anchor_count");
        var poseRows = ReadInt(row, "pose_rows");
        var distinctPoseArchives = ReadInt(row, "distinct_pose_archives");
        var distinctPoseSymbols = ReadInt(row, "distinct_pose_symbols");
        var distinctNormalizedNames = ReadInt(row, "distinct_normalized_names");
        var morphLayoutRows = ReadInt(row, "morph_layout_rows");
        var morphParseValidRows = ReadInt(row, "morph_parse_valid_rows");
        var morphContainingLayouts = ReadInt(row, "morph_containing_layouts");
        var morphContainingPairs = ReadInt(row, "morph_containing_pairs");
        var morphUniqueContainmentLayouts = ReadInt(row, "morph_unique_containment_layouts");
        var morphAmbiguousContainmentLayouts = ReadInt(row, "morph_ambiguous_containment_layouts");
        var morphExactGapSetPairs = ReadInt(row, "morph_exact_gap_set_pairs");
        var poseRowsWithoutCatalogJoinKey = ReadInt(row, "pose_rows_without_catalog_join_key");
        var poseRowsWithoutMorphJoinKey = ReadInt(row, "pose_rows_without_morph_join_key");
        var poseBankDomainSizeBefore = ReadInt(row, "pose_bank_domain_size_before");
        var poseBankDomainSizeAfter = ReadInt(row, "pose_bank_domain_size_after");
        var globalAssignmentModelsBefore = ReadInt(row, "global_assignment_models_before");
        var globalAssignmentModelsAfter = ReadInt(row, "global_assignment_models_after");
        var poseBankDomainReducedRows = ReadInt(row, "pose_bank_domain_reduced_rows");
        var uniquePoseAnimationBindingProvenRows = ReadInt(row, "unique_pose_animation_binding_proven_rows");
        var validPoseRows = ReadInt(row, "valid_pose_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-animation-catalog-pose-timing-global-assignment-control", StringComparison.Ordinal) &&
                    catalogBankCount == 19 &&
                    catalogRows == 1584 &&
                    catalogTimestampValidRows == catalogRows &&
                    anchorBankRows == 19 &&
                    anchorCompleteBankRows == anchorBankRows &&
                    anchorCount == 3168 &&
                    poseRows == 391 &&
                    distinctPoseArchives == poseRows &&
                    distinctPoseSymbols == 1 &&
                    distinctNormalizedNames == 1 &&
                    morphLayoutRows == 393 &&
                    morphParseValidRows == morphLayoutRows &&
                    morphContainingLayouts == 53 &&
                    morphContainingPairs == 159 &&
                    morphUniqueContainmentLayouts == 8 &&
                    morphAmbiguousContainmentLayouts == 45 &&
                    morphExactGapSetPairs == 0 &&
                    poseRowsWithoutCatalogJoinKey == poseRows &&
                    poseRowsWithoutMorphJoinKey == poseRows &&
                    poseBankDomainSizeBefore == 19 &&
                    poseBankDomainSizeAfter == poseBankDomainSizeBefore &&
                    globalAssignmentModelsBefore == 7429 &&
                    globalAssignmentModelsAfter == globalAssignmentModelsBefore &&
                    poseBankDomainReducedRows == 0 &&
                    uniquePoseAnimationBindingProvenRows == 0 &&
                    validPoseRows == poseRows &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            catalogBankCount,
            catalogRows,
            catalogTimestampValidRows,
            anchorBankRows,
            anchorCompleteBankRows,
            anchorCount,
            poseRows,
            distinctPoseArchives,
            distinctPoseSymbols,
            distinctNormalizedNames,
            morphLayoutRows,
            morphParseValidRows,
            morphContainingLayouts,
            morphContainingPairs,
            morphUniqueContainmentLayouts,
            morphAmbiguousContainmentLayouts,
            morphExactGapSetPairs,
            poseRowsWithoutCatalogJoinKey,
            poseRowsWithoutMorphJoinKey,
            poseBankDomainSizeBefore,
            poseBankDomainSizeAfter,
            globalAssignmentModelsBefore,
            globalAssignmentModelsAfter,
            poseBankDomainReducedRows,
            uniquePoseAnimationBindingProvenRows,
            validPoseRows,
            status,
            semanticBinding,
            provenance,
            originalSemantics);
    }

    private static OriginalStateGeometryPayloadJoinControlEvidence
        ReadOriginalStateGeometryPayloadJoinControlEvidence(string path)
    {
        const string expectedHeader =
            "adjacency_source_report\tjoin_source_report\tadjacency_rows\tjoin_rows\tjoined_object_rows\tmissing_join_objects\tadjacency_input_status_valid_rows\tjoin_input_status_valid_rows\tidentity_join_match_rows\trender_status_valid_rows\tpayload_status_valid_rows\tstructural_counts_valid_rows\tprimitive_count_match_rows\tcomplete_command_count_match_rows\tfetch_count_match_rows\tfive_slot_payload_count_match_rows\tdistinct_payload_range_count_match_rows\tpayload_overlap_zero_rows\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var adjacencyRows = ReadInt(row, "adjacency_rows");
        var joinRows = ReadInt(row, "join_rows");
        var joinedObjectRows = ReadInt(row, "joined_object_rows");
        var missingJoinObjects = ReadInt(row, "missing_join_objects");
        var adjacencyInputStatusValidRows = ReadInt(row, "adjacency_input_status_valid_rows");
        var joinInputStatusValidRows = ReadInt(row, "join_input_status_valid_rows");
        var identityJoinMatchRows = ReadInt(row, "identity_join_match_rows");
        var renderStatusValidRows = ReadInt(row, "render_status_valid_rows");
        var payloadStatusValidRows = ReadInt(row, "payload_status_valid_rows");
        var structuralCountsValidRows = ReadInt(row, "structural_counts_valid_rows");
        var primitiveCountMatchRows = ReadInt(row, "primitive_count_match_rows");
        var completeCommandCountMatchRows = ReadInt(row, "complete_command_count_match_rows");
        var fetchCountMatchRows = ReadInt(row, "fetch_count_match_rows");
        var fiveSlotPayloadCountMatchRows = ReadInt(row, "five_slot_payload_count_match_rows");
        var distinctPayloadRangeCountMatchRows = ReadInt(row, "distinct_payload_range_count_match_rows");
        var payloadOverlapZeroRows = ReadInt(row, "payload_overlap_zero_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-state-geometry-payload-join-control", StringComparison.Ordinal) &&
                    adjacencyRows > 0 &&
                    joinRows > 0 &&
                    joinedObjectRows == adjacencyRows &&
                    missingJoinObjects == 0 &&
                    adjacencyInputStatusValidRows == adjacencyRows &&
                    joinInputStatusValidRows == joinRows &&
                    identityJoinMatchRows == joinedObjectRows &&
                    renderStatusValidRows == joinedObjectRows &&
                    payloadStatusValidRows == joinedObjectRows &&
                    structuralCountsValidRows == joinedObjectRows &&
                    primitiveCountMatchRows == joinedObjectRows &&
                    completeCommandCountMatchRows == joinedObjectRows &&
                    fetchCountMatchRows == joinedObjectRows &&
                    fiveSlotPayloadCountMatchRows == joinedObjectRows &&
                    distinctPayloadRangeCountMatchRows == joinedObjectRows &&
                    payloadOverlapZeroRows == joinedObjectRows &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            adjacencyRows,
            joinRows,
            joinedObjectRows,
            missingJoinObjects,
            adjacencyInputStatusValidRows,
            joinInputStatusValidRows,
            identityJoinMatchRows,
            renderStatusValidRows,
            payloadStatusValidRows,
            structuralCountsValidRows,
            primitiveCountMatchRows,
            completeCommandCountMatchRows,
            fetchCountMatchRows,
            fiveSlotPayloadCountMatchRows,
            distinctPayloadRangeCountMatchRows,
            payloadOverlapZeroRows,
            status,
            provenance,
            originalSemantics);
    }

    private static AudioParallelRuntimeJoinControlEvidence ReadAudioParallelRuntimeJoinControlEvidence(
        string path,
        IReadOnlyDictionary<string, string>? row)
    {
        const string expectedHeader =
            "parallel_source_report\truntime_source_report\tparallel_rows\truntime_rows\tjoined_rows\tmissing_runtime_names\tmissing_parallel_names\tparallel_unique_event_names\truntime_unique_event_names\theader_row_match_rows\tdata_offset_match_rows\tdata_length_match_rows\tcandidate_metadata_validated_rows\truntime_prepared_rows\twave_header_valid_rows\tparallel_input_status_rows\truntime_input_status_rows\tstatus\tprovenance\toriginal_semantics";
        var parallelRows = ReadInt(row, "parallel_rows");
        var runtimeRows = ReadInt(row, "runtime_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var missingRuntimeNames = ReadInt(row, "missing_runtime_names");
        var missingParallelNames = ReadInt(row, "missing_parallel_names");
        var parallelUniqueEventNames = ReadInt(row, "parallel_unique_event_names");
        var runtimeUniqueEventNames = ReadInt(row, "runtime_unique_event_names");
        var headerRowMatchRows = ReadInt(row, "header_row_match_rows");
        var dataOffsetMatchRows = ReadInt(row, "data_offset_match_rows");
        var dataLengthMatchRows = ReadInt(row, "data_length_match_rows");
        var candidateMetadataValidatedRows = ReadInt(row, "candidate_metadata_validated_rows");
        var runtimePreparedRows = ReadInt(row, "runtime_prepared_rows");
        var waveHeaderValidRows = ReadInt(row, "wave_header_valid_rows");
        var parallelInputStatusRows = ReadInt(row, "parallel_input_status_rows");
        var runtimeInputStatusRows = ReadInt(row, "runtime_input_status_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-audio-event-parallel-runtime-join-control", StringComparison.Ordinal) &&
                    parallelRows == 17 &&
                    runtimeRows == parallelRows &&
                    joinedRows == parallelRows &&
                    missingRuntimeNames == 0 &&
                    missingParallelNames == 0 &&
                    parallelUniqueEventNames == parallelRows &&
                    runtimeUniqueEventNames == runtimeRows &&
                    headerRowMatchRows == joinedRows &&
                    dataOffsetMatchRows == joinedRows &&
                    dataLengthMatchRows == joinedRows &&
                    candidateMetadataValidatedRows == joinedRows &&
                    runtimePreparedRows == joinedRows &&
                    waveHeaderValidRows == joinedRows &&
                    parallelInputStatusRows == parallelRows &&
                    runtimeInputStatusRows == runtimeRows &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            parallelRows,
            runtimeRows,
            joinedRows,
            missingRuntimeNames,
            missingParallelNames,
            parallelUniqueEventNames,
            runtimeUniqueEventNames,
            headerRowMatchRows,
            dataOffsetMatchRows,
            dataLengthMatchRows,
            candidateMetadataValidatedRows,
            runtimePreparedRows,
            waveHeaderValidRows,
            parallelInputStatusRows,
            runtimeInputStatusRows,
            status,
            provenance,
            originalSemantics);
    }

    private static AudioEventEvtPcpRangeDiscriminationEvidence ReadAudioEventEvtPcpRangeDiscriminationEvidence(
        string path,
        IReadOnlyDictionary<string, string>? row)
    {
        const string expectedHeader =
            "parallel_source_report\tindex_source_report\truntime_join_source_report\tparallel_rows\tvalid_named_range_rows\tevent_string_anchor_rows\theader_range_rows\tdata_range_rows\tparallel_row_index_rows\tdata_schl_rows\tunique_event_name_rows\tunique_event_string_offset_rows\tunique_header_row_rows\tparallel_status_valid_rows\tindex_candidate_count\tindex_distinct_string_offsets\tindex_distinct_following_words\tindex_string_hash_family_candidates\tindex_candidate_names_contains_event_rows\tindex_32bit_abk_zero\tindex_32bit_pcpahdr_zero\tindex_32bit_pcpadat_zero\truntime_joined_rows\truntime_missing_names\tstatus\tprovenance\tdirect_selection_binding\toriginal_semantics";
        var parallelRows = ReadInt(row, "parallel_rows");
        var validNamedRangeRows = ReadInt(row, "valid_named_range_rows");
        var eventStringAnchorRows = ReadInt(row, "event_string_anchor_rows");
        var headerRangeRows = ReadInt(row, "header_range_rows");
        var dataRangeRows = ReadInt(row, "data_range_rows");
        var parallelRowIndexRows = ReadInt(row, "parallel_row_index_rows");
        var dataSchlRows = ReadInt(row, "data_schl_rows");
        var uniqueEventNameRows = ReadInt(row, "unique_event_name_rows");
        var uniqueEventStringOffsetRows = ReadInt(row, "unique_event_string_offset_rows");
        var uniqueHeaderRowRows = ReadInt(row, "unique_header_row_rows");
        var parallelStatusValidRows = ReadInt(row, "parallel_status_valid_rows");
        var indexCandidateNamesContainsEventRows = ReadInt(row, "index_candidate_names_contains_event_rows");
        var index32BitAbkZero = string.Equals(Get(row, "index_32bit_abk_zero"), "True", StringComparison.Ordinal);
        var index32BitPcpahdrZero = string.Equals(Get(row, "index_32bit_pcpahdr_zero"), "True", StringComparison.Ordinal);
        var index32BitPcpadatZero = string.Equals(Get(row, "index_32bit_pcpadat_zero"), "True", StringComparison.Ordinal);
        var runtimeJoinedRows = ReadInt(row, "runtime_joined_rows");
        var runtimeMissingNames = ReadInt(row, "runtime_missing_names");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var directSelectionBinding = Get(row, "direct_selection_binding");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-audio-event-evt-pcp-range-discrimination-control", StringComparison.Ordinal) &&
                    parallelRows == 17 &&
                    validNamedRangeRows == parallelRows &&
                    eventStringAnchorRows == parallelRows &&
                    headerRangeRows == parallelRows &&
                    dataRangeRows == parallelRows &&
                    parallelRowIndexRows == parallelRows &&
                    dataSchlRows == parallelRows &&
                    uniqueEventNameRows == parallelRows &&
                    uniqueEventStringOffsetRows == parallelRows &&
                    uniqueHeaderRowRows == parallelRows &&
                    parallelStatusValidRows == parallelRows &&
                    indexCandidateNamesContainsEventRows == parallelRows &&
                    index32BitAbkZero &&
                    index32BitPcpahdrZero &&
                    index32BitPcpadatZero &&
                    runtimeJoinedRows == parallelRows &&
                    runtimeMissingNames == 0 &&
                    string.Equals(directSelectionBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            parallelRows,
            validNamedRangeRows,
            eventStringAnchorRows,
            headerRangeRows,
            dataRangeRows,
            parallelRowIndexRows,
            dataSchlRows,
            uniqueEventNameRows,
            uniqueEventStringOffsetRows,
            uniqueHeaderRowRows,
            parallelStatusValidRows,
            indexCandidateNamesContainsEventRows,
            index32BitAbkZero,
            index32BitPcpahdrZero,
            index32BitPcpadatZero,
            runtimeJoinedRows,
            runtimeMissingNames,
            status,
            provenance,
            directSelectionBinding,
            originalSemantics);
    }

    private static AudioEventAudioAssetHashUniverseEvidence ReadAudioEventAudioAssetHashUniverseEvidence(
        string path,
        IReadOnlyDictionary<string, string>? row)
    {
        var eventPath = Get(row, "event_path");
        var candidateCount = ReadInt(row, "candidate_count");
        var scannedAssetCount = ReadInt(row, "scanned_asset_count");
        var validAssetCount = ReadInt(row, "valid_asset_count");
        var scannedBytes = ReadLong(row, "scanned_bytes");
        var excludedSourceRows = ReadInt(row, "excluded_source_rows");
        var assetsWithHits = ReadInt(row, "assets_with_hits");
        var assetHashHitPairs = ReadInt(row, "asset_hash_hit_pairs");
        var alignedOccurrences = ReadInt(row, "aligned_occurrences");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var directSelectionBinding = Get(row, "direct_selection_binding");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, "field\tvalue") &&
                    string.Equals(eventPath, AudioEventAudioAssetHashSearchAudit.EventPath, StringComparison.Ordinal) &&
                    string.Equals(status, AudioEventAudioAssetHashSearchAudit.Status, StringComparison.Ordinal) &&
                    candidateCount == 17 &&
                    scannedAssetCount > 0 &&
                    validAssetCount == scannedAssetCount &&
                    scannedBytes > 0 &&
                    excludedSourceRows == 1 &&
                    assetsWithHits == 0 &&
                    assetHashHitPairs == 0 &&
                    alignedOccurrences == 0 &&
                    string.Equals(provenance, AudioEventAudioAssetHashSearchAudit.Provenance, StringComparison.Ordinal) &&
                    string.Equals(directSelectionBinding, AudioEventAudioAssetHashSearchAudit.DirectSelectionBinding, StringComparison.Ordinal) &&
                    string.Equals(originalSemantics, AudioEventAudioAssetHashSearchAudit.OriginalSemantics, StringComparison.Ordinal);
        return new(
            valid,
            candidateCount,
            scannedAssetCount,
            validAssetCount,
            scannedBytes,
            excludedSourceRows,
            assetsWithHits,
            assetHashHitPairs,
            alignedOccurrences,
            status,
            provenance,
            directSelectionBinding,
            originalSemantics);
    }

    private static AudioEventDataAssetHashUniverseEvidence ReadAudioEventDataAssetHashUniverseEvidence(
        string path,
        IReadOnlyDictionary<string, string>? row)
    {
        var assetRoot = Get(row, "asset_root");
        var eventPath = Get(row, "event_path");
        var candidateCount = ReadInt(row, "candidate_count");
        var scannedAssetCount = ReadInt(row, "scanned_asset_count");
        var validAssetCount = ReadInt(row, "valid_asset_count");
        var scannedBytes = ReadLong(row, "scanned_bytes");
        var excludedSourceRows = ReadInt(row, "excluded_source_rows");
        var assetsWithHits = ReadInt(row, "assets_with_hits");
        var assetHashHitPairs = ReadInt(row, "asset_hash_hit_pairs");
        var alignedOccurrences = ReadInt(row, "aligned_occurrences");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var directSelectionBinding = Get(row, "direct_selection_binding");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, "field\tvalue") &&
                    string.Equals(assetRoot, AudioEventDataAssetHashSearchAudit.AssetRoot, StringComparison.Ordinal) &&
                    string.Equals(eventPath, AudioEventDataAssetHashSearchAudit.EventPath, StringComparison.Ordinal) &&
                    string.Equals(status, AudioEventDataAssetHashSearchAudit.Status, StringComparison.Ordinal) &&
                    candidateCount == 17 &&
                    scannedAssetCount > 60 &&
                    validAssetCount == scannedAssetCount &&
                    scannedBytes > 1737085606 &&
                    excludedSourceRows == 1 &&
                    string.Equals(provenance, AudioEventDataAssetHashSearchAudit.Provenance, StringComparison.Ordinal) &&
                    string.Equals(directSelectionBinding, AudioEventDataAssetHashSearchAudit.DirectSelectionBinding, StringComparison.Ordinal) &&
                    string.Equals(originalSemantics, AudioEventDataAssetHashSearchAudit.OriginalSemantics, StringComparison.Ordinal);
        return new(
            valid,
            assetRoot,
            eventPath,
            candidateCount,
            scannedAssetCount,
            validAssetCount,
            scannedBytes,
            excludedSourceRows,
            assetsWithHits,
            assetHashHitPairs,
            alignedOccurrences,
            status,
            provenance,
            directSelectionBinding,
            originalSemantics);
    }

    private static AudioEventDataAssetHashUniverseControlEvidence ReadAudioEventDataAssetHashUniverseControlEvidence(
        string path,
        IReadOnlyDictionary<string, string>? row)
    {
        var assetRoot = Get(row, "asset_root");
        var eventPath = Get(row, "event_path");
        var candidateCount = ReadInt(row, "candidate_count");
        var rows = ReadInt(row, "rows");
        var validRows = ReadInt(row, "valid_rows");
        var uniquePaths = ReadInt(row, "unique_paths");
        var scannedBytes = ReadLong(row, "scanned_bytes");
        var recomputedBytes = ReadLong(row, "recomputed_bytes");
        var excludedSourceRows = ReadInt(row, "excluded_source_rows");
        var assetsWithHits = ReadInt(row, "assets_with_hits");
        var hitRows = ReadInt(row, "hit_rows");
        var assetHashHitPairs = ReadInt(row, "asset_hash_hit_pairs");
        var alignedOccurrences = ReadInt(row, "aligned_occurrences");
        var hitAssetPath = Get(row, "hit_asset_path");
        var hitHashWord = Get(row, "hit_hash_word");
        var hitEventName = Get(row, "hit_event_name");
        var hitOffset = Get(row, "hit_offset");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var directSelectionBinding = Get(row, "direct_selection_binding");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, "field\tvalue") &&
                    string.Equals(assetRoot, AudioEventDataAssetHashSearchAudit.AssetRoot, StringComparison.Ordinal) &&
                    string.Equals(eventPath, AudioEventDataAssetHashSearchAudit.EventPath, StringComparison.Ordinal) &&
                    string.Equals(status, "verified-independent-audio-event-data-asset-hash-universe-control", StringComparison.Ordinal) &&
                    candidateCount == 17 &&
                    rows == 121 &&
                    validRows == rows &&
                    uniquePaths == rows &&
                    scannedBytes == 2008984464 &&
                    recomputedBytes == scannedBytes &&
                    excludedSourceRows == 1 &&
                    assetsWithHits == 1 &&
                    hitRows == 1 &&
                    assetHashHitPairs == 1 &&
                    alignedOccurrences == 1 &&
                    string.Equals(hitAssetPath, "data/movies/advoffn.vp6", StringComparison.Ordinal) &&
                    string.Equals(hitHashWord, "0x34214F6B", StringComparison.Ordinal) &&
                    string.Equals(hitEventName, "3panna_goal", StringComparison.Ordinal) &&
                    string.Equals(hitOffset, "0xA58D88", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(directSelectionBinding, "not-proven", StringComparison.Ordinal) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            assetRoot,
            eventPath,
            candidateCount,
            rows,
            validRows,
            uniquePaths,
            scannedBytes,
            recomputedBytes,
            excludedSourceRows,
            assetsWithHits,
            hitRows,
            assetHashHitPairs,
            alignedOccurrences,
            hitAssetPath,
            hitHashWord,
            hitEventName,
            hitOffset,
            status,
            provenance,
            directSelectionBinding,
            originalSemantics);
    }

    private static AudioEventNameTextSearchEvidence ReadAudioEventNameTextSearchEvidence(
        string path,
        IReadOnlyDictionary<string, string>? values)
    {
        var eventPath = Get(values, "event_path");
        var candidateCount = ReadInt(values, "candidate_count");
        var scannedAssetCount = ReadInt(values, "scanned_asset_count");
        var validAssetCount = ReadInt(values, "valid_asset_count");
        var scannedBytes = ReadLong(values, "scanned_bytes");
        var excludedSourceRows = ReadInt(values, "excluded_source_rows");
        var assetNameHitPairs = ReadInt(values, "asset_name_hit_pairs");
        var assetsWithNameHits = ReadInt(values, "assets_with_name_hits");
        var rawOccurrences = ReadInt(values, "raw_occurrences");
        var status = Get(values, "status");
        var provenance = Get(values, "provenance");
        var directSelectionBinding = Get(values, "direct_selection_binding");
        var originalSemantics = Get(values, "original_semantics");
        var valid = HasHeader(path, "field\tvalue") &&
                    string.Equals(eventPath, AudioEventNameAssetTextSearchAudit.EventPath, StringComparison.Ordinal) &&
                    candidateCount == 17 &&
                    scannedAssetCount == 60 &&
                    validAssetCount == scannedAssetCount &&
                    scannedBytes == 1737085606 &&
                    excludedSourceRows == 1 &&
                    assetNameHitPairs == 0 &&
                    assetsWithNameHits == 0 &&
                    rawOccurrences == 0 &&
                    string.Equals(status, AudioEventNameAssetTextSearchAudit.Status, StringComparison.Ordinal) &&
                    string.Equals(provenance, AudioEventNameAssetTextSearchAudit.Provenance, StringComparison.Ordinal) &&
                    string.Equals(directSelectionBinding, AudioEventNameAssetTextSearchAudit.DirectSelectionBinding, StringComparison.Ordinal) &&
                    string.Equals(originalSemantics, AudioEventNameAssetTextSearchAudit.OriginalSemantics, StringComparison.Ordinal);
        return new(
            valid,
            candidateCount,
            scannedAssetCount,
            validAssetCount,
            scannedBytes,
            excludedSourceRows,
            assetNameHitPairs,
            assetsWithNameHits,
            rawOccurrences,
            status,
            provenance,
            directSelectionBinding,
            originalSemantics);
    }

    private static AudioEventNameTextSearchControlEvidence ReadAudioEventNameTextSearchControlEvidence(
        string path,
        IReadOnlyDictionary<string, string>? row)
    {
        var inputRows = ReadInt(row, "input_rows");
        var distinctAssets = ReadInt(row, "distinct_assets");
        var distinctNames = ReadInt(row, "distinct_names");
        var validRows = ReadInt(row, "valid_rows");
        var noHitRows = ReadInt(row, "no_hit_rows");
        var rawOccurrenceRows = ReadInt(row, "raw_occurrence_rows");
        var totalRawOccurrences = ReadInt(row, "total_raw_occurrences");
        var duplicateAssetNameKeys = ReadInt(row, "duplicate_asset_name_keys");
        var sourceSummaryMatchesRecomputed = string.Equals(
            Get(row, "source_summary_matches_recomputed"),
            "True",
            StringComparison.OrdinalIgnoreCase);
        var expectedAssets = ReadInt(row, "expected_assets");
        var expectedNames = ReadInt(row, "expected_names");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var directSelectionBinding = Get(row, "direct_selection_binding");
        var originalSemantics = Get(row, "original_semantics");
        const string header = "source_report\tsource_summary\tinput_rows\tdistinct_assets\tdistinct_names\tvalid_rows\tno_hit_rows\traw_occurrence_rows\ttotal_raw_occurrences\tduplicate_asset_name_keys\tsource_summary_matches_recomputed\texpected_assets\texpected_names\tstatus\tsemantic_binding\tprovenance\tdirect_selection_binding\toriginal_semantics";
        var valid = HasHeader(path, header) &&
                    inputRows == 1020 &&
                    distinctAssets == 60 &&
                    distinctNames == 17 &&
                    validRows == inputRows &&
                    noHitRows == inputRows &&
                    rawOccurrenceRows == 0 &&
                    totalRawOccurrences == 0 &&
                    duplicateAssetNameKeys == 0 &&
                    sourceSummaryMatchesRecomputed &&
                    expectedAssets == 60 &&
                    expectedNames == 17 &&
                    string.Equals(status, AudioEventNameAssetTextSearchAudit.Status, StringComparison.Ordinal) &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    string.Equals(provenance, "independent-TSV-reparse+complete-audio-asset/name-matrix+zero-name-hit-reconciliation;no-sample-or-playback-promotion", StringComparison.Ordinal) &&
                    string.Equals(directSelectionBinding, AudioEventNameAssetTextSearchAudit.DirectSelectionBinding, StringComparison.Ordinal) &&
                    string.Equals(originalSemantics, AudioEventNameAssetTextSearchAudit.OriginalSemantics, StringComparison.Ordinal);
        return new(
            valid,
            inputRows,
            distinctAssets,
            distinctNames,
            validRows,
            noHitRows,
            rawOccurrenceRows,
            totalRawOccurrences,
            duplicateAssetNameKeys,
            sourceSummaryMatchesRecomputed,
            expectedAssets,
            expectedNames,
            status,
            semanticBinding,
            provenance,
            directSelectionBinding,
            originalSemantics);
    }

    private static AudioEventAbkRawIdentityEvidence ReadAudioEventAbkRawIdentityEvidence(
        string path,
        string controlPath,
        IReadOnlyDictionary<string, string>? row,
        IReadOnlyDictionary<string, string>? controlRow)
    {
        const string expectedHeader = "field\tvalue";
        var eventCandidates = ReadInt(row, "event_candidates");
        var eventRawRows = ReadInt(row, "event_raw_rows");
        var eventDistinctRawHashes = ReadInt(row, "event_distinct_raw_hashes");
        var bankCount = ReadInt(row, "bank_count");
        var bankNonDummySoundEntries = ReadInt(row, "bank_non_dummy_sound_entries");
        var bankAstRangeRows = ReadInt(row, "bank_ast_range_rows");
        var bankAstSchlRows = ReadInt(row, "bank_ast_schl_rows");
        var fullRawHashPairs = ReadInt(row, "full_raw_hash_pairs");
        var eventHashesWithFullRawMatch = ReadInt(row, "event_hashes_with_full_raw_match");
        var rawPrefixPairs = ReadInt(row, "raw_prefix_pairs");
        var rejectedRows = ReadInt(row, "rejected_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var controlStatus = Get(controlRow, "status");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    HasHeader(controlPath, expectedHeader) &&
                    string.Equals(status, AudioEventAbkRawIdentityAudit.Status, StringComparison.Ordinal) &&
                    string.Equals(controlStatus, "verified-independent-audio-event-abk-raw-identity-control", StringComparison.Ordinal) &&
                    eventCandidates == 17 &&
                    eventRawRows == eventCandidates &&
                    eventDistinctRawHashes == eventCandidates &&
                    bankCount == 16 &&
                    bankNonDummySoundEntries == 270 &&
                    bankAstRangeRows > 0 &&
                    bankAstSchlRows == bankAstRangeRows &&
                    fullRawHashPairs == 0 &&
                    eventHashesWithFullRawMatch == 0 &&
                    rawPrefixPairs >= 0 &&
                    rejectedRows == 0 &&
                    ReadInt(controlRow, "event_rows") == eventCandidates &&
                    ReadInt(controlRow, "event_valid_rows") == eventCandidates &&
                    ReadInt(controlRow, "ast_rows") == bankAstRangeRows &&
                    ReadInt(controlRow, "ast_valid_rows") == bankAstRangeRows &&
                    ReadInt(controlRow, "full_raw_hash_pairs") == fullRawHashPairs &&
                    ReadInt(controlRow, "event_hashes_with_full_raw_match") == eventHashesWithFullRawMatch &&
                    ReadInt(controlRow, "rejected_rows") == rejectedRows &&
                    string.Equals(originalSemantics, AudioEventAbkRawIdentityAudit.OriginalSemantics, StringComparison.Ordinal);
        return new(
            valid,
            eventCandidates,
            eventRawRows,
            eventDistinctRawHashes,
            bankCount,
            bankNonDummySoundEntries,
            bankAstRangeRows,
            bankAstSchlRows,
            fullRawHashPairs,
            eventHashesWithFullRawMatch,
            rawPrefixPairs,
            rejectedRows,
            status,
            provenance,
            controlStatus,
            originalSemantics);
    }

    private static AuthoredAudioCandidateRouteControlEvidence ReadAuthoredAudioCandidateRouteControlEvidence(
        string path,
        IReadOnlyDictionary<string, string>? row)
    {
        const string expectedHeader =
            "coverage_source_report\truntime_source_report\tscenario_rows\tverified_scenario_rows\ttrace_rows\ttrace_played_rows\troute_rows\tunique_action_rows\tunique_candidate_rows\truntime_rows\truntime_prepared_route_rows\truntime_wave_header_valid_route_rows\tmissing_runtime_candidates\tcoverage_missing_policy_actions\tstatus\tprovenance\toriginal_semantics";
        var scenarioRows = ReadInt(row, "scenario_rows");
        var verifiedScenarioRows = ReadInt(row, "verified_scenario_rows");
        var traceRows = ReadInt(row, "trace_rows");
        var tracePlayedRows = ReadInt(row, "trace_played_rows");
        var routeRows = ReadInt(row, "route_rows");
        var uniqueActionRows = ReadInt(row, "unique_action_rows");
        var uniqueCandidateRows = ReadInt(row, "unique_candidate_rows");
        var runtimeRows = ReadInt(row, "runtime_rows");
        var runtimePreparedRouteRows = ReadInt(row, "runtime_prepared_route_rows");
        var runtimeWaveHeaderValidRouteRows = ReadInt(row, "runtime_wave_header_valid_route_rows");
        var missingRuntimeCandidates = ReadInt(row, "missing_runtime_candidates");
        var coverageMissingPolicyActions = ReadInt(row, "coverage_missing_policy_actions");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-authored-audio-candidate-route-control", StringComparison.Ordinal) &&
                    scenarioRows == 10 &&
                    verifiedScenarioRows == scenarioRows &&
                    traceRows == 54 &&
                    tracePlayedRows == traceRows &&
                    routeRows == 15 &&
                    uniqueActionRows == 15 &&
                    uniqueCandidateRows == 8 &&
                    runtimeRows == 17 &&
                    runtimePreparedRouteRows == routeRows &&
                    runtimeWaveHeaderValidRouteRows == routeRows &&
                    missingRuntimeCandidates == 0 &&
                    coverageMissingPolicyActions == 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            scenarioRows,
            verifiedScenarioRows,
            traceRows,
            tracePlayedRows,
            routeRows,
            uniqueActionRows,
            uniqueCandidateRows,
            runtimeRows,
            runtimePreparedRouteRows,
            runtimeWaveHeaderValidRouteRows,
            missingRuntimeCandidates,
            coverageMissingPolicyActions,
            status,
            provenance,
            originalSemantics);
    }

    private static AuthoredNamedPayloadIdentityControlEvidence ReadAuthoredNamedPayloadIdentityControlEvidence(
        string path,
        IReadOnlyDictionary<string, string>? row)
    {
        const string expectedHeader =
            "coverage_source_report\truntime_source_report\tscenario_rows\tverified_scenario_rows\ttrace_rows\ttrace_played_rows\tidentity_rows\truntime_rows\truntime_prepared_route_rows\truntime_wave_header_valid_route_rows\twave_bytes_match_rows\tpcm_bytes_match_rows\tpcm_sha256_match_rows\tinvalid_trace_identity_rows\tmissing_runtime_candidates\tstatus\tprovenance\tdirect_selection_binding\toriginal_semantics";
        var scenarioRows = ReadInt(row, "scenario_rows");
        var verifiedScenarioRows = ReadInt(row, "verified_scenario_rows");
        var traceRows = ReadInt(row, "trace_rows");
        var tracePlayedRows = ReadInt(row, "trace_played_rows");
        var identityRows = ReadInt(row, "identity_rows");
        var runtimeRows = ReadInt(row, "runtime_rows");
        var runtimePreparedRouteRows = ReadInt(row, "runtime_prepared_route_rows");
        var runtimeWaveHeaderValidRouteRows = ReadInt(row, "runtime_wave_header_valid_route_rows");
        var waveBytesMatchRows = ReadInt(row, "wave_bytes_match_rows");
        var pcmBytesMatchRows = ReadInt(row, "pcm_bytes_match_rows");
        var pcmSha256MatchRows = ReadInt(row, "pcm_sha256_match_rows");
        var invalidTraceIdentityRows = ReadInt(row, "invalid_trace_identity_rows");
        var missingRuntimeCandidates = ReadInt(row, "missing_runtime_candidates");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var directSelectionBinding = Get(row, "direct_selection_binding");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-authored-audio-named-payload-identity-control", StringComparison.Ordinal) &&
                    scenarioRows == 10 &&
                    verifiedScenarioRows == scenarioRows &&
                    traceRows == 54 &&
                    tracePlayedRows == traceRows &&
                    identityRows == traceRows &&
                    runtimeRows == 17 &&
                    runtimePreparedRouteRows == 8 &&
                    runtimeWaveHeaderValidRouteRows == runtimePreparedRouteRows &&
                    waveBytesMatchRows == identityRows &&
                    pcmBytesMatchRows == identityRows &&
                    pcmSha256MatchRows == identityRows &&
                    invalidTraceIdentityRows == 0 &&
                    missingRuntimeCandidates == 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(directSelectionBinding, "not-proven", StringComparison.Ordinal) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            scenarioRows,
            verifiedScenarioRows,
            traceRows,
            tracePlayedRows,
            identityRows,
            runtimeRows,
            runtimePreparedRouteRows,
            runtimeWaveHeaderValidRouteRows,
            waveBytesMatchRows,
            pcmBytesMatchRows,
            pcmSha256MatchRows,
            invalidTraceIdentityRows,
            missingRuntimeCandidates,
            status,
            provenance,
            directSelectionBinding,
            originalSemantics);
    }

    private static AudioBankDecoderSelectionControlEvidence ReadAudioBankDecoderSelectionControlEvidence(
        string path,
        IReadOnlyDictionary<string, string>? row)
    {
        const string expectedHeader =
            "batch_source_report\texhaustive_source_report\tbatch_rows\texhaustive_bank_rows\tunique_batch_banks\tunique_exhaustive_banks\tbatch_decoded_rows\tbatch_rejected_rows\tselected_identity_rows\tselected_kind_match_rows\tselected_wave_bytes_match_rows\trejected_policy_match_rows\tvalid_control_rows\tstatus\tprovenance\tdirect_selection_binding\toriginal_semantics";
        var batchRows = ReadInt(row, "batch_rows");
        var exhaustiveBankRows = ReadInt(row, "exhaustive_bank_rows");
        var uniqueBatchBanks = ReadInt(row, "unique_batch_banks");
        var uniqueExhaustiveBanks = ReadInt(row, "unique_exhaustive_banks");
        var batchDecodedRows = ReadInt(row, "batch_decoded_rows");
        var batchRejectedRows = ReadInt(row, "batch_rejected_rows");
        var selectedIdentityRows = ReadInt(row, "selected_identity_rows");
        var selectedKindMatchRows = ReadInt(row, "selected_kind_match_rows");
        var selectedWaveBytesMatchRows = ReadInt(row, "selected_wave_bytes_match_rows");
        var rejectedPolicyMatchRows = ReadInt(row, "rejected_policy_match_rows");
        var validControlRows = ReadInt(row, "valid_control_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var directSelectionBinding = Get(row, "direct_selection_binding");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-audio-bank-decoder-selection-control", StringComparison.Ordinal) &&
                    batchRows == 16 &&
                    exhaustiveBankRows == 242 &&
                    uniqueBatchBanks == 16 &&
                    uniqueExhaustiveBanks == 16 &&
                    batchDecodedRows == 15 &&
                    batchRejectedRows == 1 &&
                    selectedIdentityRows == 15 &&
                    selectedKindMatchRows == 15 &&
                    selectedWaveBytesMatchRows == 15 &&
                    rejectedPolicyMatchRows == 1 &&
                    validControlRows == 16 &&
                    string.Equals(directSelectionBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            batchRows,
            exhaustiveBankRows,
            uniqueBatchBanks,
            uniqueExhaustiveBanks,
            batchDecodedRows,
            batchRejectedRows,
            selectedIdentityRows,
            selectedKindMatchRows,
            selectedWaveBytesMatchRows,
            rejectedPolicyMatchRows,
            validControlRows,
            status,
            provenance,
            directSelectionBinding,
            originalSemantics);
    }

    private static Fifast2CsiEvtNameAmbiguityControlEvidence
        ReadFifast2CsiEvtNameAmbiguityControlEvidence(
            string path,
            IReadOnlyDictionary<string, string>? row)
    {
        const string expectedHeader =
            "csi_source_report\tevt_source_report\tcsi_rows\tevt_rows\tcsi_valid_rows\tevt_valid_rows\tevt_unique_names\tevt_rows_with_zero_csi_name_matches\tevt_rows_with_single_csi_name_match\tevt_rows_with_ambiguous_csi_name_matches\tevt_rows_with_any_csi_name_match\texact_event_name_overlap_rows\tunique_name_binding_proven_rows\tstatus\tsemantic_binding\tdirect_selection_binding\tprovenance\toriginal_semantics";
        var csiRows = ReadInt(row, "csi_rows");
        var evtRows = ReadInt(row, "evt_rows");
        var csiValidRows = ReadInt(row, "csi_valid_rows");
        var evtValidRows = ReadInt(row, "evt_valid_rows");
        var evtUniqueNames = ReadInt(row, "evt_unique_names");
        var evtRowsWithZeroCsiNameMatches = ReadInt(row, "evt_rows_with_zero_csi_name_matches");
        var evtRowsWithSingleCsiNameMatch = ReadInt(row, "evt_rows_with_single_csi_name_match");
        var evtRowsWithAmbiguousCsiNameMatches = ReadInt(row, "evt_rows_with_ambiguous_csi_name_matches");
        var evtRowsWithAnyCsiNameMatch = ReadInt(row, "evt_rows_with_any_csi_name_match");
        var exactEventNameOverlapRows = ReadInt(row, "exact_event_name_overlap_rows");
        var uniqueNameBindingProvenRows = ReadInt(row, "unique_name_binding_proven_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var directSelectionBinding = Get(row, "direct_selection_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-fifast2-csi-evt-name-ambiguity-control", StringComparison.Ordinal) &&
                    csiRows == 100 &&
                    evtRows == 17 &&
                    csiValidRows == csiRows &&
                    evtValidRows == evtRows &&
                    evtUniqueNames == evtRows &&
                    evtRowsWithZeroCsiNameMatches == 10 &&
                    evtRowsWithSingleCsiNameMatch == 3 &&
                    evtRowsWithAmbiguousCsiNameMatches == 4 &&
                    evtRowsWithAnyCsiNameMatch == 7 &&
                    exactEventNameOverlapRows == 0 &&
                    uniqueNameBindingProvenRows == 0 &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    string.Equals(directSelectionBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            csiRows,
            evtRows,
            csiValidRows,
            evtValidRows,
            evtUniqueNames,
            evtRowsWithZeroCsiNameMatches,
            evtRowsWithSingleCsiNameMatch,
            evtRowsWithAmbiguousCsiNameMatches,
            evtRowsWithAnyCsiNameMatch,
            exactEventNameOverlapRows,
            uniqueNameBindingProvenRows,
            status,
            semanticBinding,
            directSelectionBinding,
            provenance,
            originalSemantics);
    }

    private static AudioEventStaticContextGlobalAssignmentControlEvidence
        ReadAudioEventStaticContextGlobalAssignmentControlEvidence(string path)
    {
        const string expectedHeader =
            "static_report\tstatic_summary_report\tevent_path_summary_report\tevent_payload_summary_report\tstatic_audio_xref_rows\tunique_audio_literals\taudio_context_rows\taudio_context_with_executable_calls\tdistinct_executable_call_targets\taudio_targets_with_multiple_literals\tnested_audio_literal_matches\txdvdfs_audio_literal_matches\taudio_event_literal_xref_rows\tevent_path_candidate_name_count\tevent_path_candidate_pairs\tevent_path_literal_rows\tevent_path_literal_occurrences\tevent_path_literal_xref_count\tevent_path_matched_rows\tevent_path_distinct_matched_paths\tevent_rows\tbank_candidate_pool_size\tevent_exact_pcm_match_rows\tevent_hashes_with_match\tunique_evt_abkc_sample_binding_proven_rows\tevent_rows_without_static_event_xref\tstatic_event_context_target_pool_size\tevent_static_candidate_model_sum_before\tevent_static_candidate_model_sum_after\tevent_static_context_reduced_rows\tunique_event_static_selector_proven_rows\tvalid_event_rows\tstatus\tsemantic_binding\tdirect_selection_binding\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var staticAudioXrefRows = ReadInt(row, "static_audio_xref_rows");
        var uniqueAudioLiterals = ReadInt(row, "unique_audio_literals");
        var audioContextRows = ReadInt(row, "audio_context_rows");
        var audioContextWithExecutableCalls = ReadInt(row, "audio_context_with_executable_calls");
        var distinctExecutableCallTargets = ReadInt(row, "distinct_executable_call_targets");
        var audioTargetsWithMultipleLiterals = ReadInt(row, "audio_targets_with_multiple_literals");
        var nestedAudioLiteralMatches = ReadInt(row, "nested_audio_literal_matches");
        var xdvdfsAudioLiteralMatches = ReadInt(row, "xdvdfs_audio_literal_matches");
        var audioEventLiteralXrefRows = ReadInt(row, "audio_event_literal_xref_rows");
        var eventPathCandidateNameCount = ReadInt(row, "event_path_candidate_name_count");
        var eventPathCandidatePairs = ReadInt(row, "event_path_candidate_pairs");
        var eventPathLiteralRows = ReadInt(row, "event_path_literal_rows");
        var eventPathLiteralOccurrences = ReadInt(row, "event_path_literal_occurrences");
        var eventPathLiteralXrefCount = ReadInt(row, "event_path_literal_xref_count");
        var eventPathMatchedRows = ReadInt(row, "event_path_matched_rows");
        var eventPathDistinctMatchedPaths = ReadInt(row, "event_path_distinct_matched_paths");
        var eventRows = ReadInt(row, "event_rows");
        var bankCandidatePoolSize = ReadInt(row, "bank_candidate_pool_size");
        var eventExactPcmMatchRows = ReadInt(row, "event_exact_pcm_match_rows");
        var eventHashesWithMatch = ReadInt(row, "event_hashes_with_match");
        var uniqueEvtAbkcSampleBindingProvenRows = ReadInt(row, "unique_evt_abkc_sample_binding_proven_rows");
        var eventRowsWithoutStaticEventXref = ReadInt(row, "event_rows_without_static_event_xref");
        var staticEventContextTargetPoolSize = ReadInt(row, "static_event_context_target_pool_size");
        var eventStaticCandidateModelSumBefore = ReadInt(row, "event_static_candidate_model_sum_before");
        var eventStaticCandidateModelSumAfter = ReadInt(row, "event_static_candidate_model_sum_after");
        var eventStaticContextReducedRows = ReadInt(row, "event_static_context_reduced_rows");
        var uniqueEventStaticSelectorProvenRows = ReadInt(row, "unique_event_static_selector_proven_rows");
        var validEventRows = ReadInt(row, "valid_event_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var directSelectionBinding = Get(row, "direct_selection_binding");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-audio-event-static-context-global-assignment-control", StringComparison.Ordinal) &&
                    staticAudioXrefRows == 58 &&
                    uniqueAudioLiterals == 23 &&
                    audioContextRows == 147 &&
                    audioContextWithExecutableCalls == 108 &&
                    distinctExecutableCallTargets == 56 &&
                    audioTargetsWithMultipleLiterals == 14 &&
                    nestedAudioLiteralMatches == 1 &&
                    xdvdfsAudioLiteralMatches == 10 &&
                    audioEventLiteralXrefRows == 0 &&
                    eventPathCandidateNameCount == 17 &&
                    eventPathCandidatePairs == 1061 &&
                    eventPathLiteralRows == 4 &&
                    eventPathLiteralOccurrences == 9 &&
                    eventPathLiteralXrefCount == 0 &&
                    eventPathMatchedRows == 4 &&
                    eventPathDistinctMatchedPaths == 1 &&
                    eventRows == 17 &&
                    bankCandidatePoolSize == 241 &&
                    eventExactPcmMatchRows == 0 &&
                    eventHashesWithMatch == 0 &&
                    uniqueEvtAbkcSampleBindingProvenRows == 0 &&
                    eventRowsWithoutStaticEventXref == eventRows &&
                    staticEventContextTargetPoolSize == 56 &&
                    eventStaticCandidateModelSumBefore == 952 &&
                    eventStaticCandidateModelSumAfter == eventStaticCandidateModelSumBefore &&
                    eventStaticContextReducedRows == 0 &&
                    uniqueEventStaticSelectorProvenRows == 0 &&
                    validEventRows == eventRows &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    string.Equals(directSelectionBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            staticAudioXrefRows,
            uniqueAudioLiterals,
            audioContextRows,
            audioContextWithExecutableCalls,
            distinctExecutableCallTargets,
            audioTargetsWithMultipleLiterals,
            nestedAudioLiteralMatches,
            xdvdfsAudioLiteralMatches,
            audioEventLiteralXrefRows,
            eventPathCandidateNameCount,
            eventPathCandidatePairs,
            eventPathLiteralRows,
            eventPathLiteralOccurrences,
            eventPathLiteralXrefCount,
            eventPathMatchedRows,
            eventPathDistinctMatchedPaths,
            eventRows,
            bankCandidatePoolSize,
            eventExactPcmMatchRows,
            eventHashesWithMatch,
            uniqueEvtAbkcSampleBindingProvenRows,
            eventRowsWithoutStaticEventXref,
            staticEventContextTargetPoolSize,
            eventStaticCandidateModelSumBefore,
            eventStaticCandidateModelSumAfter,
            eventStaticContextReducedRows,
            uniqueEventStaticSelectorProvenRows,
            validEventRows,
            status,
            semanticBinding,
            directSelectionBinding,
            provenance,
            originalSemantics);
    }

    private static AudioEventSampleGlobalAssignmentControlEvidence
        ReadAudioEventSampleGlobalAssignmentControlEvidence(
            string path,
            IReadOnlyDictionary<string, string>? row)
    {
        const string expectedHeader =
            "payload_source_report\tcsi_source_report\tparallel_source_report\tnegative_synthesis_source_report\tauthored_route_source_report\tevent_rows\tbank_rows\tdecoded_event_rows\tdecoded_bank_rows\tevent_input_status_valid_rows\tcsi_input_status_valid_rows\tparallel_input_status_valid_rows\tnegative_synthesis_valid_source_count\tauthored_route_rows\tbank_candidate_pool_size\tevent_exact_pcm_match_rows\tevent_hashes_with_match\tevt_rows_with_zero_exact_sample_models\tevt_rows_with_single_weak_name_models\tevt_rows_with_multiple_weak_name_models\tevt_rows_with_zero_weak_name_models\tweak_name_domain_sum\texact_identity_model_sum\tstructural_cross_product_model_sum\tunique_evt_abkc_sample_binding_proven_rows\tvalid_event_rows\tstatus\tsemantic_binding\tprovenance\tdirect_selection_binding\toriginal_semantics";
        var eventRows = ReadInt(row, "event_rows");
        var bankRows = ReadInt(row, "bank_rows");
        var decodedEventRows = ReadInt(row, "decoded_event_rows");
        var decodedBankRows = ReadInt(row, "decoded_bank_rows");
        var eventInputStatusValidRows = ReadInt(row, "event_input_status_valid_rows");
        var csiInputStatusValidRows = ReadInt(row, "csi_input_status_valid_rows");
        var parallelInputStatusValidRows = ReadInt(row, "parallel_input_status_valid_rows");
        var negativeSynthesisValidSourceCount = ReadInt(row, "negative_synthesis_valid_source_count");
        var authoredRouteRows = ReadInt(row, "authored_route_rows");
        var bankCandidatePoolSize = ReadInt(row, "bank_candidate_pool_size");
        var eventExactPcmMatchRows = ReadInt(row, "event_exact_pcm_match_rows");
        var eventHashesWithMatch = ReadInt(row, "event_hashes_with_match");
        var eventRowsWithZeroExactSampleModels = ReadInt(row, "evt_rows_with_zero_exact_sample_models");
        var eventRowsWithSingleWeakNameModels = ReadInt(row, "evt_rows_with_single_weak_name_models");
        var eventRowsWithMultipleWeakNameModels = ReadInt(row, "evt_rows_with_multiple_weak_name_models");
        var eventRowsWithZeroWeakNameModels = ReadInt(row, "evt_rows_with_zero_weak_name_models");
        var weakNameDomainSum = ReadInt(row, "weak_name_domain_sum");
        var exactIdentityModelSum = ReadInt(row, "exact_identity_model_sum");
        var structuralCrossProductModelSum = ReadInt(row, "structural_cross_product_model_sum");
        var uniqueEvtAbkcSampleBindingProvenRows = ReadInt(row, "unique_evt_abkc_sample_binding_proven_rows");
        var validEventRows = ReadInt(row, "valid_event_rows");
        var status = Get(row, "status");
        var semanticBinding = Get(row, "semantic_binding");
        var provenance = Get(row, "provenance");
        var directSelectionBinding = Get(row, "direct_selection_binding");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-original-audio-event-sample-global-assignment-control", StringComparison.Ordinal) &&
                    eventRows == 17 &&
                    bankRows == 242 &&
                    decodedEventRows == 17 &&
                    decodedBankRows == 241 &&
                    eventInputStatusValidRows == 17 &&
                    csiInputStatusValidRows == 17 &&
                    parallelInputStatusValidRows == 17 &&
                    negativeSynthesisValidSourceCount == 5 &&
                    authoredRouteRows == 15 &&
                    bankCandidatePoolSize == 241 &&
                    eventExactPcmMatchRows == 0 &&
                    eventHashesWithMatch == 0 &&
                    eventRowsWithZeroExactSampleModels == 17 &&
                    eventRowsWithSingleWeakNameModels == 3 &&
                    eventRowsWithMultipleWeakNameModels == 4 &&
                    eventRowsWithZeroWeakNameModels == 10 &&
                    weakNameDomainSum == 38 &&
                    exactIdentityModelSum == 0 &&
                    structuralCrossProductModelSum == 0 &&
                    uniqueEvtAbkcSampleBindingProvenRows == 0 &&
                    validEventRows == 17 &&
                    string.Equals(semanticBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(directSelectionBinding, "not-proven", StringComparison.Ordinal) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            eventRows,
            bankRows,
            decodedEventRows,
            decodedBankRows,
            eventInputStatusValidRows,
            csiInputStatusValidRows,
            parallelInputStatusValidRows,
            negativeSynthesisValidSourceCount,
            authoredRouteRows,
            bankCandidatePoolSize,
            eventExactPcmMatchRows,
            eventHashesWithMatch,
            eventRowsWithZeroExactSampleModels,
            eventRowsWithSingleWeakNameModels,
            eventRowsWithMultipleWeakNameModels,
            eventRowsWithZeroWeakNameModels,
            weakNameDomainSum,
            exactIdentityModelSum,
            structuralCrossProductModelSum,
            uniqueEvtAbkcSampleBindingProvenRows,
            validEventRows,
            status,
            semanticBinding,
            provenance,
            directSelectionBinding,
            originalSemantics);
    }

    private static AudioPayloadPolicyControlEvidence ReadAudioPayloadPolicyControlEvidence(
        string path,
        IReadOnlyDictionary<string, string>? row)
    {
        const string expectedHeader =
            "bounded_source_report\tall_source_report\tbounded_rows\tall_rows\tjoined_rows\tbounded_event_rows\tall_event_rows\tbounded_bank_rows\tall_bank_rows\tbounded_input_status_rows\tall_input_status_rows\tidentity_match_rows\tstatus_match_rows\tdecode_match_rows\twave_bytes_match_rows\tpcm_hash_match_rows\texact_match_count_match_rows\texact_pcm_match_rows\tstatus\tprovenance\toriginal_semantics";
        var boundedRows = ReadInt(row, "bounded_rows");
        var allRows = ReadInt(row, "all_rows");
        var joinedRows = ReadInt(row, "joined_rows");
        var boundedEventRows = ReadInt(row, "bounded_event_rows");
        var allEventRows = ReadInt(row, "all_event_rows");
        var boundedBankRows = ReadInt(row, "bounded_bank_rows");
        var allBankRows = ReadInt(row, "all_bank_rows");
        var identityMatchRows = ReadInt(row, "identity_match_rows");
        var statusMatchRows = ReadInt(row, "status_match_rows");
        var decodeMatchRows = ReadInt(row, "decode_match_rows");
        var waveBytesMatchRows = ReadInt(row, "wave_bytes_match_rows");
        var pcmHashMatchRows = ReadInt(row, "pcm_hash_match_rows");
        var exactMatchCountMatchRows = ReadInt(row, "exact_match_count_match_rows");
        var exactPcmMatchRows = ReadInt(row, "exact_pcm_match_rows");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-audio-event-payload-policy-control", StringComparison.Ordinal) &&
                    boundedRows > 0 &&
                    allRows == boundedRows &&
                    joinedRows == boundedRows &&
                    boundedEventRows == allEventRows &&
                    boundedBankRows == allBankRows &&
                    identityMatchRows == joinedRows &&
                    statusMatchRows == joinedRows &&
                    decodeMatchRows == joinedRows &&
                    waveBytesMatchRows == joinedRows &&
                    pcmHashMatchRows == joinedRows &&
                    exactMatchCountMatchRows == joinedRows &&
                    exactPcmMatchRows == 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            boundedRows,
            allRows,
            joinedRows,
            boundedEventRows,
            allEventRows,
            boundedBankRows,
            allBankRows,
            identityMatchRows,
            statusMatchRows,
            decodeMatchRows,
            waveBytesMatchRows,
            pcmHashMatchRows,
            exactMatchCountMatchRows,
            exactPcmMatchRows,
            status,
            provenance,
            originalSemantics);
    }

    private static AudioNamedPayloadIdentityControlEvidence
        ReadAudioNamedPayloadIdentityControlEvidence(
            string path,
            IReadOnlyDictionary<string, string>? row)
    {
        const string expectedHeader =
            "runtime_source_report\tnamed_decode_source_report\tcorrelation_source_report\truntime_rows\tnamed_decode_rows\tcorrelation_event_rows\tcorrelation_bank_rows\tcorrelation_bank_decoded_rows\tjoined_event_rows\tmissing_runtime_events\tmissing_named_decode_events\tmissing_correlation_events\truntime_metadata_validated_rows\truntime_prepared_rows\truntime_wave_header_valid_rows\tnamed_decode_validated_rows\tcorrelation_decode_validated_rows\twave_bytes_match_rows\tpcm_bytes_match_rows\tpcm_sha256_match_rows\tevent_hashes_with_bank_match\texact_pcm_hash_pairs\tunique_event_names\tstatus\tprovenance\tdirect_selection_binding\toriginal_semantics";
        var runtimeRows = ReadInt(row, "runtime_rows");
        var namedDecodeRows = ReadInt(row, "named_decode_rows");
        var correlationEventRows = ReadInt(row, "correlation_event_rows");
        var correlationBankRows = ReadInt(row, "correlation_bank_rows");
        var correlationBankDecodedRows = ReadInt(row, "correlation_bank_decoded_rows");
        var joinedEventRows = ReadInt(row, "joined_event_rows");
        var missingRuntimeEvents = ReadInt(row, "missing_runtime_events");
        var missingNamedDecodeEvents = ReadInt(row, "missing_named_decode_events");
        var missingCorrelationEvents = ReadInt(row, "missing_correlation_events");
        var runtimeMetadataValidatedRows = ReadInt(row, "runtime_metadata_validated_rows");
        var runtimePreparedRows = ReadInt(row, "runtime_prepared_rows");
        var runtimeWaveHeaderValidRows = ReadInt(row, "runtime_wave_header_valid_rows");
        var namedDecodeValidatedRows = ReadInt(row, "named_decode_validated_rows");
        var correlationDecodeValidatedRows = ReadInt(row, "correlation_decode_validated_rows");
        var waveBytesMatchRows = ReadInt(row, "wave_bytes_match_rows");
        var pcmBytesMatchRows = ReadInt(row, "pcm_bytes_match_rows");
        var pcmSha256MatchRows = ReadInt(row, "pcm_sha256_match_rows");
        var eventHashesWithBankMatch = ReadInt(row, "event_hashes_with_bank_match");
        var exactPcmHashPairs = ReadInt(row, "exact_pcm_hash_pairs");
        var uniqueEventNames = ReadInt(row, "unique_event_names");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var directSelectionBinding = Get(row, "direct_selection_binding");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-independent-audio-event-named-payload-identity-control", StringComparison.Ordinal) &&
                    runtimeRows == 17 &&
                    namedDecodeRows == 17 &&
                    correlationEventRows == 17 &&
                    correlationBankRows == 242 &&
                    correlationBankDecodedRows == 241 &&
                    joinedEventRows == 17 &&
                    missingRuntimeEvents == 0 &&
                    missingNamedDecodeEvents == 0 &&
                    missingCorrelationEvents == 0 &&
                    runtimeMetadataValidatedRows == 17 &&
                    runtimePreparedRows == 17 &&
                    runtimeWaveHeaderValidRows == 17 &&
                    namedDecodeValidatedRows == 17 &&
                    correlationDecodeValidatedRows == 17 &&
                    waveBytesMatchRows == 17 &&
                    pcmBytesMatchRows == 17 &&
                    pcmSha256MatchRows == 17 &&
                    eventHashesWithBankMatch == 0 &&
                    exactPcmHashPairs == 0 &&
                    uniqueEventNames == 17 &&
                    string.Equals(directSelectionBinding, "not-proven", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            runtimeRows,
            namedDecodeRows,
            correlationEventRows,
            correlationBankRows,
            correlationBankDecodedRows,
            joinedEventRows,
            missingRuntimeEvents,
            missingNamedDecodeEvents,
            missingCorrelationEvents,
            runtimeMetadataValidatedRows,
            runtimePreparedRows,
            runtimeWaveHeaderValidRows,
            namedDecodeValidatedRows,
            correlationDecodeValidatedRows,
            waveBytesMatchRows,
            pcmBytesMatchRows,
            pcmSha256MatchRows,
            eventHashesWithBankMatch,
            exactPcmHashPairs,
            uniqueEventNames,
            status,
            provenance,
            directSelectionBinding,
            originalSemantics);
    }

    private static StaticRenderIndirectIndexedCfgEvidence ReadStaticRenderIndirectIndexedCfgEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcandidate_targets\tanalyzed_targets\ttargets_with_reachable_cfg\ttargets_with_reachable_ret\ttotal_linear_instructions\ttotal_reachable_instructions\ttotal_basic_blocks\ttotal_conditional_branches\ttotal_unconditional_branches\ttotal_internal_branch_targets\ttotal_out_of_window_branch_targets\ttotal_direct_calls\ttotal_unique_direct_call_targets\tdistinct_direct_call_targets\ttotal_indirect_control_transfers\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var candidateTargets = ReadInt(row, "candidate_targets");
        var analyzedTargets = ReadInt(row, "analyzed_targets");
        var targetsWithReachableCfg = ReadInt(row, "targets_with_reachable_cfg");
        var targetsWithReachableRet = ReadInt(row, "targets_with_reachable_ret");
        var totalLinearInstructions = ReadInt(row, "total_linear_instructions");
        var totalReachableInstructions = ReadInt(row, "total_reachable_instructions");
        var totalBasicBlocks = ReadInt(row, "total_basic_blocks");
        var totalConditionalBranches = ReadInt(row, "total_conditional_branches");
        var totalUnconditionalBranches = ReadInt(row, "total_unconditional_branches");
        var totalInternalBranchTargets = ReadInt(row, "total_internal_branch_targets");
        var totalOutOfWindowBranchTargets = ReadInt(row, "total_out_of_window_branch_targets");
        var totalDirectCalls = ReadInt(row, "total_direct_calls");
        var totalUniqueDirectCallTargets = ReadInt(row, "total_unique_direct_call_targets");
        var distinctDirectCallTargets = ReadInt(row, "distinct_direct_call_targets");
        var totalIndirectControlTransfers = ReadInt(row, "total_indirect_control_transfers");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-bounded-static-render-indirect-indexed-cfg-slices", StringComparison.Ordinal) &&
                    candidateTargets > 0 &&
                    analyzedTargets == candidateTargets &&
                    targetsWithReachableCfg == analyzedTargets &&
                    targetsWithReachableRet > 0 &&
                    totalLinearInstructions >= totalReachableInstructions &&
                    totalReachableInstructions > 0 &&
                    totalBasicBlocks > 0 &&
                    totalConditionalBranches >= 0 &&
                    totalUnconditionalBranches >= 0 &&
                    totalInternalBranchTargets >= 0 &&
                    totalOutOfWindowBranchTargets >= 0 &&
                    totalDirectCalls >= 0 &&
                    totalUniqueDirectCallTargets >= 0 &&
                    distinctDirectCallTargets >= 0 &&
                    distinctDirectCallTargets <= totalUniqueDirectCallTargets &&
                    totalIndirectControlTransfers >= 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            candidateTargets,
            analyzedTargets,
            targetsWithReachableCfg,
            targetsWithReachableRet,
            totalLinearInstructions,
            totalReachableInstructions,
            totalBasicBlocks,
            totalConditionalBranches,
            totalUnconditionalBranches,
            totalInternalBranchTargets,
            totalOutOfWindowBranchTargets,
            totalDirectCalls,
            totalUniqueDirectCallTargets,
            distinctDirectCallTargets,
            totalIndirectControlTransfers,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderIndirectIndexedCfgCallTargetEvidence ReadStaticRenderIndirectIndexedCfgCallTargetEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcfg_direct_call_occurrences\tcfg_unique_direct_call_targets_sum\tdistinct_direct_call_targets\tmapped_executable_targets\tdisassembled_targets\tret_terminated_targets\tprologue_like_targets\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var cfgDirectCallOccurrences = ReadInt(row, "cfg_direct_call_occurrences");
        var cfgUniqueDirectCallTargetsSum = ReadInt(row, "cfg_unique_direct_call_targets_sum");
        var distinctDirectCallTargets = ReadInt(row, "distinct_direct_call_targets");
        var mappedExecutableTargets = ReadInt(row, "mapped_executable_targets");
        var disassembledTargets = ReadInt(row, "disassembled_targets");
        var retTerminatedTargets = ReadInt(row, "ret_terminated_targets");
        var prologueLikeTargets = ReadInt(row, "prologue_like_targets");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-bounded-static-render-indirect-indexed-cfg-call-target-slices", StringComparison.Ordinal) &&
                    cfgDirectCallOccurrences >= 0 &&
                    cfgUniqueDirectCallTargetsSum >= 0 &&
                    distinctDirectCallTargets > 0 &&
                    distinctDirectCallTargets <= cfgUniqueDirectCallTargetsSum &&
                    mappedExecutableTargets == distinctDirectCallTargets &&
                    disassembledTargets == mappedExecutableTargets &&
                    retTerminatedTargets >= 0 &&
                    retTerminatedTargets <= disassembledTargets &&
                    prologueLikeTargets >= 0 &&
                    prologueLikeTargets <= disassembledTargets &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            cfgDirectCallOccurrences,
            cfgUniqueDirectCallTargetsSum,
            distinctDirectCallTargets,
            mappedExecutableTargets,
            disassembledTargets,
            retTerminatedTargets,
            prologueLikeTargets,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderIndirectIndexedCfgCallSiteEvidence ReadStaticRenderIndirectIndexedCfgCallSiteEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tsource_cfg_candidates\tdirect_call_occurrences\tdistinct_call_targets\tcandidate_targets_with_call_sites\tcall_sites_with_push_edx_before_lea\tcall_sites_with_lea_ecx_base_before_call\tcall_sites_with_ret_imm_4_after_call\tdistinct_lea_displacements\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var sourceCfgCandidates = ReadInt(row, "source_cfg_candidates");
        var directCallOccurrences = ReadInt(row, "direct_call_occurrences");
        var distinctCallTargets = ReadInt(row, "distinct_call_targets");
        var candidateTargetsWithCallSites = ReadInt(row, "candidate_targets_with_call_sites");
        var callSitesWithPushEdxBeforeLea = ReadInt(row, "call_sites_with_push_edx_before_lea");
        var callSitesWithLeaEcxBaseBeforeCall = ReadInt(row, "call_sites_with_lea_ecx_base_before_call");
        var callSitesWithRetImm4AfterCall = ReadInt(row, "call_sites_with_ret_imm_4_after_call");
        var distinctLeaDisplacements = ReadInt(row, "distinct_lea_displacements");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-bounded-static-render-indirect-indexed-call-sites", StringComparison.Ordinal) &&
                    sourceCfgCandidates > 0 &&
                    directCallOccurrences > 0 &&
                    distinctCallTargets > 0 &&
                    candidateTargetsWithCallSites > 0 &&
                    callSitesWithPushEdxBeforeLea >= 0 &&
                    callSitesWithLeaEcxBaseBeforeCall >= 0 &&
                    callSitesWithRetImm4AfterCall >= 0 &&
                    distinctLeaDisplacements >= 0 &&
                    callSitesWithPushEdxBeforeLea <= directCallOccurrences &&
                    callSitesWithLeaEcxBaseBeforeCall <= directCallOccurrences &&
                    callSitesWithRetImm4AfterCall <= directCallOccurrences &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            sourceCfgCandidates,
            directCallOccurrences,
            distinctCallTargets,
            candidateTargetsWithCallSites,
            callSitesWithPushEdxBeforeLea,
            callSitesWithLeaEcxBaseBeforeCall,
            callSitesWithRetImm4AfterCall,
            distinctLeaDisplacements,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence ReadStaticRenderIndirectIndexedCfgNestedCallTargetCfgEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tparent_target_rows\tparent_direct_call_occurrences\tdistinct_nested_targets\tanalyzed_targets\ttargets_with_reachable_cfg\ttargets_with_reachable_ret\ttotal_linear_instructions\ttotal_reachable_instructions\ttotal_basic_blocks\ttotal_conditional_branches\ttotal_unconditional_branches\ttotal_internal_branch_targets\ttotal_out_of_window_branch_targets\ttotal_direct_calls\ttotal_unique_direct_call_targets\tdistinct_nested_direct_call_targets\ttotal_indirect_control_transfers\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var parentTargetRows = ReadInt(row, "parent_target_rows");
        var parentDirectCallOccurrences = ReadInt(row, "parent_direct_call_occurrences");
        var distinctNestedTargets = ReadInt(row, "distinct_nested_targets");
        var analyzedTargets = ReadInt(row, "analyzed_targets");
        var targetsWithReachableCfg = ReadInt(row, "targets_with_reachable_cfg");
        var targetsWithReachableRet = ReadInt(row, "targets_with_reachable_ret");
        var totalLinearInstructions = ReadInt(row, "total_linear_instructions");
        var totalReachableInstructions = ReadInt(row, "total_reachable_instructions");
        var totalBasicBlocks = ReadInt(row, "total_basic_blocks");
        var totalConditionalBranches = ReadInt(row, "total_conditional_branches");
        var totalUnconditionalBranches = ReadInt(row, "total_unconditional_branches");
        var totalInternalBranchTargets = ReadInt(row, "total_internal_branch_targets");
        var totalOutOfWindowBranchTargets = ReadInt(row, "total_out_of_window_branch_targets");
        var totalDirectCalls = ReadInt(row, "total_direct_calls");
        var totalUniqueDirectCallTargets = ReadInt(row, "total_unique_direct_call_targets");
        var distinctNestedDirectCallTargets = ReadInt(row, "distinct_nested_direct_call_targets");
        var totalIndirectControlTransfers = ReadInt(row, "total_indirect_control_transfers");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-bounded-static-render-indirect-indexed-cfg-nested-call-target-cfg-slices", StringComparison.Ordinal) &&
                    parentTargetRows > 0 &&
                    parentDirectCallOccurrences > 0 &&
                    distinctNestedTargets > 0 &&
                    analyzedTargets == distinctNestedTargets &&
                    targetsWithReachableCfg == analyzedTargets &&
                    targetsWithReachableRet > 0 &&
                    totalLinearInstructions >= totalReachableInstructions &&
                    totalReachableInstructions > 0 &&
                    totalBasicBlocks > 0 &&
                    totalConditionalBranches >= 0 &&
                    totalUnconditionalBranches >= 0 &&
                    totalInternalBranchTargets >= 0 &&
                    totalOutOfWindowBranchTargets >= 0 &&
                    totalDirectCalls >= 0 &&
                    totalUniqueDirectCallTargets >= 0 &&
                    distinctNestedDirectCallTargets >= 0 &&
                    distinctNestedDirectCallTargets <= totalUniqueDirectCallTargets &&
                    totalIndirectControlTransfers >= 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            parentTargetRows,
            parentDirectCallOccurrences,
            distinctNestedTargets,
            analyzedTargets,
            targetsWithReachableCfg,
            targetsWithReachableRet,
            totalLinearInstructions,
            totalReachableInstructions,
            totalBasicBlocks,
            totalConditionalBranches,
            totalUnconditionalBranches,
            totalInternalBranchTargets,
            totalOutOfWindowBranchTargets,
            totalDirectCalls,
            totalUniqueDirectCallTargets,
            distinctNestedDirectCallTargets,
            totalIndirectControlTransfers,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderIndirectIndexedCfgNestedCallSiteEvidence ReadStaticRenderIndirectIndexedCfgNestedCallSiteEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tparent_cfg_target_rows\tnested_call_target_rows\tdistinct_nested_call_targets\tnested_call_sites_with_push_edx_before_lea\tnested_call_sites_with_lea_ecx_base_before_call\tnested_call_sites_with_ret_imm_4_after_call\tdistinct_nested_lea_displacements\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var parentCfgTargetRows = ReadInt(row, "parent_cfg_target_rows");
        var nestedCallTargetRows = ReadInt(row, "nested_call_target_rows");
        var distinctNestedCallTargets = ReadInt(row, "distinct_nested_call_targets");
        var nestedCallSitesWithPushEdxBeforeLea = ReadInt(row, "nested_call_sites_with_push_edx_before_lea");
        var nestedCallSitesWithLeaEcxBaseBeforeCall = ReadInt(row, "nested_call_sites_with_lea_ecx_base_before_call");
        var nestedCallSitesWithRetImm4AfterCall = ReadInt(row, "nested_call_sites_with_ret_imm_4_after_call");
        var distinctNestedLeaDisplacements = ReadInt(row, "distinct_nested_lea_displacements");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-bounded-static-render-indirect-indexed-nested-call-sites", StringComparison.Ordinal) &&
                    parentCfgTargetRows > 0 &&
                    nestedCallTargetRows > 0 &&
                    distinctNestedCallTargets > 0 &&
                    nestedCallSitesWithPushEdxBeforeLea >= 0 &&
                    nestedCallSitesWithLeaEcxBaseBeforeCall >= 0 &&
                    nestedCallSitesWithRetImm4AfterCall >= 0 &&
                    distinctNestedLeaDisplacements >= 0 &&
                    nestedCallSitesWithPushEdxBeforeLea <= nestedCallTargetRows &&
                    nestedCallSitesWithLeaEcxBaseBeforeCall <= nestedCallTargetRows &&
                    nestedCallSitesWithRetImm4AfterCall <= nestedCallTargetRows &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            parentCfgTargetRows,
            nestedCallTargetRows,
            distinctNestedCallTargets,
            nestedCallSitesWithPushEdxBeforeLea,
            nestedCallSitesWithLeaEcxBaseBeforeCall,
            nestedCallSitesWithRetImm4AfterCall,
            distinctNestedLeaDisplacements,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderIndirectIndexedCfgCallSiteJoinEvidence ReadStaticRenderIndirectIndexedCfgCallSiteJoinEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tindexed_table_target_rows\tdistinct_table_bases\tcall_site_rows\tjoined_call_site_rows\tdistinct_call_targets\tdistinct_table_entry_indices\ttable_entry_occurrences_for_joined_call_sites\tduplicate_table_occurrences_over_call_sites\tcall_sites_with_complete_local_shape\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var indexedTableTargetRows = ReadInt(row, "indexed_table_target_rows");
        var distinctTableBases = ReadInt(row, "distinct_table_bases");
        var callSiteRows = ReadInt(row, "call_site_rows");
        var joinedCallSiteRows = ReadInt(row, "joined_call_site_rows");
        var distinctCallTargets = ReadInt(row, "distinct_call_targets");
        var distinctTableEntryIndices = ReadInt(row, "distinct_table_entry_indices");
        var tableEntryOccurrencesForJoinedCallSites = ReadInt(row, "table_entry_occurrences_for_joined_call_sites");
        var duplicateTableOccurrencesOverCallSites = ReadInt(row, "duplicate_table_occurrences_over_call_sites");
        var callSitesWithCompleteLocalShape = ReadInt(row, "call_sites_with_complete_local_shape");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-bounded-static-render-indirect-indexed-call-site-join", StringComparison.Ordinal) &&
                    indexedTableTargetRows > 0 &&
                    distinctTableBases > 0 &&
                    callSiteRows > 0 &&
                    joinedCallSiteRows == callSiteRows &&
                    distinctCallTargets > 0 &&
                    distinctTableEntryIndices >= joinedCallSiteRows &&
                    tableEntryOccurrencesForJoinedCallSites >= joinedCallSiteRows &&
                    duplicateTableOccurrencesOverCallSites == tableEntryOccurrencesForJoinedCallSites - joinedCallSiteRows &&
                    callSitesWithCompleteLocalShape >= 0 &&
                    callSitesWithCompleteLocalShape <= joinedCallSiteRows &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            indexedTableTargetRows,
            distinctTableBases,
            callSiteRows,
            joinedCallSiteRows,
            distinctCallTargets,
            distinctTableEntryIndices,
            tableEntryOccurrencesForJoinedCallSites,
            duplicateTableOccurrencesOverCallSites,
            callSitesWithCompleteLocalShape,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderIndirectIndexedCfgCallTargetCfgEvidence ReadStaticRenderIndirectIndexedCfgCallTargetCfgEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcandidate_targets\tanalyzed_targets\ttargets_with_reachable_cfg\ttargets_with_reachable_ret\ttotal_linear_instructions\ttotal_reachable_instructions\ttotal_basic_blocks\ttotal_conditional_branches\ttotal_unconditional_branches\ttotal_internal_branch_targets\ttotal_out_of_window_branch_targets\ttotal_direct_calls\ttotal_unique_direct_call_targets\tdistinct_direct_call_targets\ttotal_indirect_control_transfers\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var candidateTargets = ReadInt(row, "candidate_targets");
        var analyzedTargets = ReadInt(row, "analyzed_targets");
        var targetsWithReachableCfg = ReadInt(row, "targets_with_reachable_cfg");
        var targetsWithReachableRet = ReadInt(row, "targets_with_reachable_ret");
        var totalLinearInstructions = ReadInt(row, "total_linear_instructions");
        var totalReachableInstructions = ReadInt(row, "total_reachable_instructions");
        var totalBasicBlocks = ReadInt(row, "total_basic_blocks");
        var totalConditionalBranches = ReadInt(row, "total_conditional_branches");
        var totalUnconditionalBranches = ReadInt(row, "total_unconditional_branches");
        var totalInternalBranchTargets = ReadInt(row, "total_internal_branch_targets");
        var totalOutOfWindowBranchTargets = ReadInt(row, "total_out_of_window_branch_targets");
        var totalDirectCalls = ReadInt(row, "total_direct_calls");
        var totalUniqueDirectCallTargets = ReadInt(row, "total_unique_direct_call_targets");
        var distinctDirectCallTargets = ReadInt(row, "distinct_direct_call_targets");
        var totalIndirectControlTransfers = ReadInt(row, "total_indirect_control_transfers");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-bounded-static-render-indirect-indexed-cfg-call-target-cfg-slices", StringComparison.Ordinal) &&
                    candidateTargets > 0 &&
                    analyzedTargets == candidateTargets &&
                    targetsWithReachableCfg == analyzedTargets &&
                    targetsWithReachableRet > 0 &&
                    totalLinearInstructions >= totalReachableInstructions &&
                    totalReachableInstructions > 0 &&
                    totalBasicBlocks > 0 &&
                    totalConditionalBranches >= 0 &&
                    totalUnconditionalBranches >= 0 &&
                    totalInternalBranchTargets >= 0 &&
                    totalOutOfWindowBranchTargets >= 0 &&
                    totalDirectCalls >= 0 &&
                    totalUniqueDirectCallTargets >= 0 &&
                    distinctDirectCallTargets >= 0 &&
                    distinctDirectCallTargets <= totalUniqueDirectCallTargets &&
                    totalIndirectControlTransfers >= 0 &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            candidateTargets,
            analyzedTargets,
            targetsWithReachableCfg,
            targetsWithReachableRet,
            totalLinearInstructions,
            totalReachableInstructions,
            totalBasicBlocks,
            totalConditionalBranches,
            totalUnconditionalBranches,
            totalInternalBranchTargets,
            totalOutOfWindowBranchTargets,
            totalDirectCalls,
            totalUniqueDirectCallTargets,
            distinctDirectCallTargets,
            totalIndirectControlTransfers,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderIndirectIndexedCfgJoinEvidence ReadStaticRenderIndirectIndexedCfgJoinEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcfg_direct_call_targets\trender_direct_call_targets\trender_window_targets\trender_call_graph_nodes\tcfg_targets_in_render_direct_call_targets\tcfg_targets_in_render_window_targets\tcfg_targets_in_render_call_graph_nodes\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var cfgDirectCallTargets = ReadInt(row, "cfg_direct_call_targets");
        var renderDirectCallTargets = ReadInt(row, "render_direct_call_targets");
        var renderWindowTargets = ReadInt(row, "render_window_targets");
        var renderCallGraphNodes = ReadInt(row, "render_call_graph_nodes");
        var cfgTargetsInRenderDirectCallTargets = ReadInt(row, "cfg_targets_in_render_direct_call_targets");
        var cfgTargetsInRenderWindowTargets = ReadInt(row, "cfg_targets_in_render_window_targets");
        var cfgTargetsInRenderCallGraphNodes = ReadInt(row, "cfg_targets_in_render_call_graph_nodes");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-structural-negative", StringComparison.Ordinal) &&
                    cfgDirectCallTargets > 0 &&
                    renderDirectCallTargets > 0 &&
                    renderWindowTargets > 0 &&
                    renderCallGraphNodes > 0 &&
                    cfgTargetsInRenderDirectCallTargets >= 0 &&
                    cfgTargetsInRenderDirectCallTargets <= cfgDirectCallTargets &&
                    cfgTargetsInRenderWindowTargets >= 0 &&
                    cfgTargetsInRenderWindowTargets <= cfgDirectCallTargets &&
                    cfgTargetsInRenderCallGraphNodes >= 0 &&
                    cfgTargetsInRenderCallGraphNodes <= cfgDirectCallTargets &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            cfgDirectCallTargets,
            renderDirectCallTargets,
            renderWindowTargets,
            renderCallGraphNodes,
            cfgTargetsInRenderDirectCallTargets,
            cfgTargetsInRenderWindowTargets,
            cfgTargetsInRenderCallGraphNodes,
            status,
            provenance,
            originalSemantics);
    }

    private static StaticRenderIndirectIndexedTargetJoinEvidence ReadStaticRenderIndirectIndexedTargetJoinEvidence(string path)
    {
        const string expectedHeader =
            "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcandidate_targets\tdirect_call_targets\trender_window_targets\tcandidate_targets_in_direct_call_targets\tcandidate_targets_in_render_window_targets\tstatus\tprovenance\toriginal_semantics";
        var rows = ReadTsvRows(path);
        var row = rows.FirstOrDefault();
        var candidateTargets = ReadInt(row, "candidate_targets");
        var directCallTargets = ReadInt(row, "direct_call_targets");
        var renderWindowTargets = ReadInt(row, "render_window_targets");
        var candidateTargetsInDirectCallTargets = ReadInt(row, "candidate_targets_in_direct_call_targets");
        var candidateTargetsInRenderWindowTargets = ReadInt(row, "candidate_targets_in_render_window_targets");
        var status = Get(row, "status");
        var provenance = Get(row, "provenance");
        var originalSemantics = Get(row, "original_semantics");
        var valid = HasHeader(path, expectedHeader) &&
                    string.Equals(status, "verified-structural-negative", StringComparison.Ordinal) &&
                    candidateTargets > 0 &&
                    directCallTargets > 0 &&
                    renderWindowTargets > 0 &&
                    candidateTargetsInDirectCallTargets >= 0 &&
                    candidateTargetsInDirectCallTargets <= candidateTargets &&
                    candidateTargetsInRenderWindowTargets >= 0 &&
                    candidateTargetsInRenderWindowTargets <= candidateTargets &&
                    !string.IsNullOrWhiteSpace(provenance) &&
                    string.Equals(originalSemantics, "not-claimed", StringComparison.Ordinal);
        return new(
            valid,
            candidateTargets,
            directCallTargets,
            renderWindowTargets,
            candidateTargetsInDirectCallTargets,
            candidateTargetsInRenderWindowTargets,
            status,
            provenance,
            originalSemantics);
    }

    private static List<Dictionary<string, string>> ReadTsvRows(string path)
    {
        if (!File.Exists(path))
        {
            return new List<Dictionary<string, string>>();
        }

        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            return new List<Dictionary<string, string>>();
        }

        var headers = lines[0].Split('\t');
        var rows = new List<Dictionary<string, string>>();
        foreach (var line in lines.Skip(1))
        {
            if (line.Length == 0)
            {
                continue;
            }

            var fields = line.Split('\t');
            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < headers.Length; index++)
            {
                row[headers[index]] = index < fields.Length ? fields[index] : string.Empty;
            }
            rows.Add(row);
        }
        return rows;
    }

    private static bool HasHeader(string path, string expectedHeader)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        var firstLine = File.ReadLines(path).FirstOrDefault();
        return string.Equals(firstLine, expectedHeader, StringComparison.Ordinal);
    }

    private static Dictionary<string, string> ReadKeyValueTsv(
        string path,
        string expectedHeader = CleanRoomIntegrationReport.Header)
    {
        if (!HasHeader(path, expectedHeader))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            if (line.Length == 0)
            {
                continue;
            }

            var fields = line.Split('\t', 2);
            if (fields.Length == 2)
            {
                values[fields[0]] = fields[1];
            }
        }

        return values;
    }

    private static string Get(IReadOnlyDictionary<string, string>? row, string key) =>
        row is not null && row.TryGetValue(key, out var value) ? value : string.Empty;

    private static int ReadInt(
        IReadOnlyDictionary<string, string>? row,
        string key)
    {
        return row is not null &&
               int.TryParse(Get(row, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static long ReadLong(
        IReadOnlyDictionary<string, string>? row,
        string key)
    {
        return row is not null &&
               long.TryParse(Get(row, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static bool IsSha256Hex(string value) =>
        value.Length == 64 && value.All(character =>
            character is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f');

    private static string Field(string value) => value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
