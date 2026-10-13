"""Smoke tests for the read-only FIFA Street 2 analysis artifacts."""

from __future__ import annotations

import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
ANALYSIS = ROOT / "tools" / "analysis"
ISO = ROOT / "FIFA Street 2 (USA) (En,Es).iso"
sys.path.insert(0, str(ANALYSIS))

from list_xdvdfs import XdvdfsReader  # noqa: E402
from parse_xbe import analyze_xbe  # noqa: E402
from parse_big_archives import inspect as inspect_big_archives  # noqa: E402
from xbe_string_xref_audit import (  # noqa: E402
    build_call_clusters,
    build_call_context,
    collect_literals,
    literal_category,
)
from xbe_asset_literal_audit import (  # noqa: E402
    is_asset_literal,
    match_literal_to_paths,
    normalize_path,
)
from xbe_asset_nested_literal_audit import (  # noqa: E402
    decode_refpack,
    match_literal_to_nested_paths,
    parse_big_index,
)
from xbe_nested_literal_xref_audit import join_rows  # noqa: E402
from xbe_audio_static_cluster_audit import (  # noqa: E402
    build_audio_clusters,
    build_audio_literal_rows,
    is_audio_literal,
)
from audio_event_asset_path_index_control import build_control  # noqa: E402
from xbe_render_static_cluster_audit import (  # noqa: E402
    build_render_clusters,
    build_render_literal_rows,
    classify_render_literal,
    is_render_literal,
)
from xbe_render_target_audit import select_targets  # noqa: E402
from xbe_render_cfg_audit import recover_cfg  # noqa: E402
from xbe_render_call_target_audit import aggregate_call_targets  # noqa: E402
from xbe_render_call_graph_audit import build_graph  # noqa: E402
from xbe_render_indirect_transfer_audit import classify_indirect_transfers  # noqa: E402
from xbe_render_indirect_reference_audit import (  # noqa: E402
    read_reference_word,
    reference_address,
)
from xbe_render_indirect_reference_slice_audit import collect_reference_words  # noqa: E402
from xbe_render_indirect_join_audit import build_join  # noqa: E402
from xbe_render_indirect_indexed_table_audit import (  # noqa: E402
    _indexed_table_bases,
    scan_table,
)
from xbe_render_indirect_indexed_target_audit import collect_indexed_targets  # noqa: E402
from xbe_render_indirect_indexed_accessor_audit import _shape  # noqa: E402
from eagl_original_state_population_control import analyze_rows  # noqa: E402
from eagl_original_vertex_record_association_control import (  # noqa: E402
    analyze_rows as analyze_vertex_record_association_rows,
)
from eagl_original_vertex_payload_control import (  # noqa: E402
    analyze_rows as analyze_vertex_payload_rows,
)
from eagl_original_numeric_stream_ambiguity_control import (  # noqa: E402
    analyze as analyze_numeric_stream_ambiguity,
)
from eagl_original_bounds_consistent_record_control import (  # noqa: E402
    analyze as analyze_bounds_consistent_record,
)
from eagl_original_bounds_fetch_slot_join_control import (  # noqa: E402
    analyze as analyze_bounds_fetch_slot_join,
)
from audio_event_parallel_runtime_join_control import (  # noqa: E402
    analyze as analyze_audio_parallel_runtime_join,
)
from audio_authored_candidate_route_control import (  # noqa: E402
    analyze as analyze_authored_audio_candidate_routes,
)
from audio_event_selection_negative_synthesis import (  # noqa: E402
    analyze as analyze_audio_selection_negative_synthesis,
)
from eagl_original_state_candidate_attribute_control import (  # noqa: E402
    analyze as analyze_state_candidate_attribute,
)
from eagl_original_shapename_candidate_asset_control import (  # noqa: E402
    analyze as analyze_shapename_candidate_asset,
)
from eagl_original_state_candidate_primitive_shape_control import (  # noqa: E402
    analyze as analyze_state_candidate_primitive_shape,
)
from eagl_original_candidate_shapename_primitive_index_control import (  # noqa: E402
    analyze as analyze_original_candidate_shapename_primitive_index,
)
from eagl_original_candidate_primitive_fetch_control import (  # noqa: E402
    analyze as analyze_original_candidate_primitive_fetch,
)
from eagl_original_candidate_stream_record_control import (  # noqa: E402
    analyze as analyze_original_candidate_stream_record,
)
from eagl_original_candidate_geometry_stream_control import (  # noqa: E402
    analyze as analyze_original_candidate_geometry_stream,
)
from eagl_original_state_token_control import (  # noqa: E402
    analyze_rows as analyze_state_token_rows,
)
from eagl_original_render_payload_object_join import (  # noqa: E402
    analyze_rows as analyze_render_payload_object_join_rows,
)
from eagl_original_primitive_stream_topology_control import (  # noqa: E402
    analyze_rows as analyze_primitive_stream_topology_rows,
)
from eagl_original_primitive_stream_opaque_discrimination_control import (  # noqa: E402
    analyze_rows as analyze_primitive_stream_opaque_discrimination_rows,
)
from eagl_original_primitive_stream_command_descriptor_adjacency_control import (  # noqa: E402
    analyze as analyze_primitive_stream_command_descriptor_adjacency,
)
from eagl_original_state_fetch_adjacency_control import (  # noqa: E402
    analyze as analyze_state_fetch_adjacency,
)
from eagl_original_primitive_fetch_topology_control import (  # noqa: E402
    analyze_rows as analyze_primitive_fetch_topology_rows,
)
from eagl_original_command_payload_region_control import (  # noqa: E402
    analyze_rows as analyze_command_payload_region_rows,
)
from xbe_render_indirect_indexed_target_join_audit import build_join as build_indexed_target_join  # noqa: E402
from xbe_render_indirect_indexed_cfg_audit import analyze_indexed_target  # noqa: E402
from xbe_render_indirect_indexed_cfg_call_site_audit import build_summary as build_indexed_cfg_call_site_summary  # noqa: E402
from xbe_render_indirect_indexed_cfg_call_target_audit import collect_call_targets  # noqa: E402
from xbe_render_indirect_indexed_cfg_call_target_cfg_audit import build_summary as build_indexed_cfg_call_target_cfg_summary  # noqa: E402
from xbe_render_indirect_indexed_cfg_call_site_join_audit import build_summary as build_indexed_cfg_call_site_join_summary  # noqa: E402
from xbe_render_indirect_indexed_cfg_call_site_join_audit import join_rows as join_indexed_cfg_call_site_rows  # noqa: E402
from xbe_render_indirect_indexed_cfg_nested_call_target_cfg_audit import (  # noqa: E402
    build_summary as build_indexed_cfg_nested_call_target_cfg_summary,
    collect_nested_targets,
)
from xbe_render_indirect_indexed_cfg_nested_call_site_audit import (  # noqa: E402
    build_summary as build_indexed_cfg_nested_call_site_summary,
    filter_nested_call_sites,
)
from xbe_render_indirect_indexed_cfg_join_audit import build_join as build_indexed_cfg_join  # noqa: E402
from xbe_nested_payload_inventory import _format_from_header, inspect_payload  # noqa: E402


class FifaAnalysisSmokeTests(unittest.TestCase):
    def test_authored_audio_candidate_routes_require_runtime_candidates(self) -> None:
        coverage = [
            {
                "kind": "scenario",
                "name": "fixture",
                "trace_path": "fixture-trace.tsv",
                "trace_valid": "True",
                "replay_matched": "True",
            },
            {
                "kind": "summary",
                "missing_policy_actions": "",
            },
        ]
        runtime = [
            {
                "event_name": "intro",
                "runtime_prepared": "True",
                "wave_header_valid": "True",
                "status": "verified-runtime-named-payload",
                "original_semantics": "not-claimed",
            }
        ]
        traces = {
            "fixture-trace.tsv": [
                {"action": "KICKOFF", "candidate": "intro", "route": "played-clean-room-candidate"}
            ]
        }
        report, summary = analyze_authored_audio_candidate_routes(coverage, runtime, traces)
        self.assertEqual(len(report), 1)
        self.assertEqual(summary["runtime_prepared_route_rows"], "1")
        self.assertEqual(summary["status"], "inconclusive-authored-audio-candidate-route-control")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_audio_selection_negative_synthesis_preserves_unproven_binding(self) -> None:
        report, summary = analyze_audio_selection_negative_synthesis(
            {
                "status": "no-direct-selection-binding",
                "string_hash_family_candidates": "1",
                "abk_aligned_occurrences": "0",
                "pcpahdr_big_aligned_occurrences": "0",
                "pcpadat_big_aligned_occurrences": "0",
                "direct_selection_binding": "not-proven",
                "original_semantics": "not-claimed",
            },
            {
                "status": "verified-structural-xbe-hash-search",
                "candidate_count": "1",
                "aligned_occurrences": "0",
                "unaligned_occurrences": "0",
                "direct_selection_binding": "not-proven",
                "original_semantics": "not-claimed",
            },
            {
                "status": "verified-structural",
                "event_candidates": "1",
                "event_decoded": "1",
                "bank_count": "1",
                "exact_pcm_hash_pairs": "0",
                "event_hashes_with_match": "0",
                "direct_selection_binding": "not-proven",
                "original_semantics": "not-claimed",
            },
            {
                "status": "verified-independent-audio-event-parallel-runtime-join-control",
                "joined_rows": "1",
                "missing_runtime_names": "0",
                "missing_parallel_names": "0",
                "original_semantics": "not-claimed",
            },
            {
                "status": "verified-independent-authored-audio-candidate-route-control",
                "route_rows": "1",
                "missing_runtime_candidates": "0",
                "coverage_missing_policy_actions": "0",
                "direct_selection_binding": "not-proven",
                "original_semantics": "not-claimed",
            },
        )
        self.assertEqual(len(report), 5)
        self.assertEqual(summary["valid_source_count"], "0")
        self.assertEqual(summary["status"], "inconclusive-verified-independent-audio-selection-negative-synthesis")
        self.assertEqual(summary["direct_selection_binding"], "not-proven")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_state_candidate_attribute_control_stays_structural(self) -> None:
        state = [
            {
                "model_archive": "tops0001.big",
                "model_object": "tops0001.ord",
                "status": "verified-bounded-original-state-token-negative-control",
                "original_semantics": "not-claimed",
                "texture_token_hits": "0",
                "material_token_hits": "0",
                "uv_coordinate_token_hits": "0",
                "vertex_attribute_token_hits": "0",
                "deformation_token_hits": "0",
            }
        ]
        attribute = [
            {
                "model_archive": "tops0001.big",
                "model_object": "tops0001.ord",
                "status": "complete-slot1-profile",
                "slot1_complete_groups": "1",
                "slot1_finite_groups": "1",
                "slot1_near_unit_groups": "1",
                "original_semantics": "not-claimed",
            }
        ]
        influence = [
            {
                "kind": "candidate",
                "model_archive": "tops0001.big",
                "model_object": "tops0001.ord",
                "status": "observed-candidate-raster-unchanged",
                "geometry_finite_and_indexed": "True",
                "candidate_texture_influences_raster": "False",
                "mesh_shape_state_exact": "False",
                "original_semantics": "not-claimed",
            }
        ]
        report, summary = analyze_state_candidate_attribute(state, attribute, influence)
        self.assertEqual(len(report), 1)
        self.assertEqual(summary["joined_object_rows"], "1")
        self.assertEqual(summary["state_negative_token_rows"], "1")
        self.assertEqual(summary["influence_mesh_shape_state_exact_rows"], "0")
        self.assertEqual(summary["status"], "inconclusive-original-state-candidate-attribute-control")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_shapename_candidate_asset_control_is_lexical(self) -> None:
        report, summary = analyze_shapename_candidate_asset(
            [
                {
                    "model_archive": "tops0030.big",
                    "object_name": "tops0030.ord",
                    "shape_name": "tops",
                    "state_names": "SHAPENAME=tops,1",
                    "object_state_exact": "True",
                    "status": "verified-control",
                }
            ],
            [
                {
                    "model_archive_name": "tops0030.big",
                    "model_object_name": "tops0030.ord",
                    "shape_name": "tops",
                    "same_trunk": "True",
                    "shape_entry_exact": "True",
                    "decoded_pixels_match": "True",
                    "status": "verified-candidate-source-chain",
                    "original_semantics": "not-claimed",
                }
            ],
        )
        self.assertEqual(len(report), 1)
        self.assertEqual(summary["shapename_token_match_rows"], "1")
        self.assertEqual(summary["same_trunk_rows"], "1")
        self.assertEqual(summary["status"], "inconclusive-verified-independent-original-shapename-candidate-asset-control")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_state_candidate_primitive_shape_control_is_structural(self) -> None:
        report, summary = analyze_state_candidate_primitive_shape(
            [
                {
                    "model_archive": "tops0030.big",
                    "object_name": "tops0030.ord",
                    "state_primitive_count": "1",
                    "state_names": "SHAPENAME=tops,1",
                    "status": "verified-control",
                }
            ],
            [
                {
                    "model_archive_name": "tops0030.big",
                    "model_object_name": "tops0030.ord",
                    "candidate_primitive_shape_state_count": "1",
                    "candidate_primitive_shape_state_streams_complete_count": "1",
                    "candidate_primitive_count": "2",
                    "same_trunk": "True",
                    "status": "verified-candidate-source-chain",
                    "shape_entry_exact": "True",
                    "decoded_pixels_match": "True",
                    "original_semantics": "not-claimed",
                }
            ],
        )
        self.assertEqual(len(report), 1)
        self.assertEqual(summary["state_primitive_count_match_rows"], "1")
        self.assertEqual(summary["shapename_count_match_rows"], "1")
        self.assertEqual(
            summary["status"],
            "inconclusive-verified-independent-original-state-candidate-primitive-shape-control",
        )
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_candidate_shapename_primitive_index_control_is_structural(self) -> None:
        report, summary = analyze_original_candidate_shapename_primitive_index(
            [
                {
                    "model_archive": "tops0030.big",
                    "object_name": "tops0030.ord",
                    "shape_name": "tops",
                    "state_primitive_indices": "0:1,0:2",
                    "status": "verified-control",
                }
            ],
            [
                {
                    "model_archive": "tops0030.big",
                    "model_object": "tops0030.ord",
                    "shape_name": "tops",
                    "candidate_shape_state_count": "2",
                    "candidate_shape_state_layers": "0,0",
                    "candidate_shape_state_indices": "1,2",
                    "candidate_shape_state_streams_complete": "True",
                    "status": "verified-candidate-shape-state-index-control",
                    "original_semantics": "not-claimed",
                }
            ],
        )
        self.assertEqual(len(report), 1)
        self.assertEqual(summary["sequence_match_rows"], "1")
        self.assertEqual(summary["set_match_rows"], "1")
        self.assertEqual(
            summary["status"],
            "inconclusive-verified-independent-original-candidate-shapename-primitive-index-control",
        )
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_candidate_primitive_fetch_control_preserves_unmaterialized_incomplete_slots(self) -> None:
        report, summary = analyze_original_candidate_primitive_fetch(
            [
                {
                    "model_archive": "tops0030.big",
                    "model_object": "tops0030.ord",
                    "layer": "0",
                    "primitive": "0",
                    "command_count": "17",
                    "command_stream_complete": "True",
                    "fetch_slot_count": "5",
                    "fetch_slot_ids": "0,1,2,3,4",
                    "complete_five_slot_fetch_sequence": "True",
                    "state_assignments": "SetPrimitiveType=EAGL::PT_TRIANGLESTRIP;SetShapeName=__Tops",
                    "has_primitive_type_assignment": "True",
                    "status": "verified-bounded-original-render-chain-structure",
                    "original_semantics": "not-claimed",
                }
            ],
            [
                {
                    "model_archive": "tops0030.big",
                    "model_object": "tops0030.ord",
                    "layer": "0",
                    "primitive": "0",
                    "command_count": "17",
                    "command_stream_complete": "True",
                    "fetch_slot_count": "5",
                    "slot_records": "slot=0;aux=0x10|slot=1;aux=0x20|slot=2;aux=0x30|slot=3;aux=0x40|slot=4;aux=0x50",
                    "complete_five_slot_layout": "True",
                    "state_names": "SetPrimitiveType=EAGL::PT_TRIANGLESTRIP|SetShapeName=__Tops",
                    "primitive_type": "PT_TRIANGLESTRIP",
                    "status": "verified-bounded-candidate-fetch-record-association",
                    "original_semantics": "not-claimed",
                }
            ],
        )
        self.assertEqual(len(report), 1)
        self.assertEqual(report[0]["fetch_slot_id_sequence_match"], "True")
        self.assertEqual(report[0]["candidate_slot_records_unmaterialized_expected"], "False")
        self.assertEqual(summary["candidate_slot_records_valid_rows"], "1")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_candidate_stream_record_control_is_raw_metadata_only(self) -> None:
        original = [
            {
                "model_archive": "tops0030.big",
                "model_object": "tops0030.ord",
                "layer": "0",
                "primitive": "0",
                "fetch_slot": str(slot),
                "fetch_auxiliary_value": aux,
                "fetch_stride": str(stride),
                "symbol_name": f"__VB:::__GPvb_{10 + slot}_0_Tops_BBJersey_model",
                "byte_length": str(stride * 132),
                "stream_stride": str(stride),
                "element_count": "132",
                "complete_five_slot_fetch_sequence": "True",
                "status": "verified-bounded-original-primitive-stream-topology",
                "original_semantics": "not-claimed",
            }
            for slot, aux, stride in (
                (0, "0x00000660", 12),
                (1, "0x00000450", 8),
                (2, "0x00000000", 4),
                (3, "0x00000660", 12),
                (4, "0x00000000", 2),
            )
        ]
        candidate = [
            {
                "model_archive": "tops0030.big",
                "model_object": "tops0030.ord",
                "layer": "0",
                "primitive": "0",
                "fetch_slot_count": "5",
                "complete_five_slot_layout": "True",
                "slot_records": "|".join(
                    f"slot={slot};aux={aux};command_stride={stride};stream_stride={stride};stream_bytes={stride * 132};elements=132;stream=__VB:::__GPvb_{10 + slot}_0_Tops_BBJersey_model;valid=True"
                    for slot, aux, stride in (
                        (0, "0x00000660", 12),
                        (1, "0x00000450", 8),
                        (2, "0x00000000", 4),
                        (3, "0x00000660", 12),
                        (4, "0x00000000", 2),
                    )
                ),
                "status": "verified-bounded-candidate-fetch-record-association",
                "original_semantics": "not-claimed",
            }
        ]
        report, summary = analyze_original_candidate_stream_record(original, candidate)
        self.assertEqual(len(report), 1)
        self.assertEqual(report[0]["record_exact_rows"], "5")
        self.assertEqual(summary["record_exact_rows"], "5")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_candidate_geometry_stream_control_is_not_a_semantic_binding(self) -> None:
        original = [
            {
                "model_archive": "tops0030.big",
                "model_object": "tops0030.ord",
                "layer": "0",
                "primitive": "0",
                "fetch_slot": "0",
                "symbol_name": "__VB:::__GPvb_10_0_Tops_BBJersey_model",
                "stream_stride": "12",
                "byte_length": "1584",
                "element_count": "132",
                "complete_five_slot_fetch_sequence": "True",
                "status": "verified-bounded-original-primitive-stream-topology",
                "original_semantics": "not-claimed",
            }
        ]
        candidate = [
            {
                "model_archive": "tops0030.big",
                "model_object": "tops0030.ord",
                "layer": "0",
                "primitive": "0",
                "candidate_source_slot": "0",
                "candidate_stream_name": "__VB:::__GPvb_10_0_Tops_BBJersey_model",
                "candidate_stream_stride": "12",
                "candidate_stream_byte_length": "1584",
                "candidate_stream_element_count": "132",
                "candidate_vertex_count": "132",
                "candidate_triangle_count": "130",
                "candidate_primitive_complete": "True",
                "candidate_geometry_finite_and_indexed": "True",
                "status": "verified-bounded-candidate-geometry-stream-audit",
                "original_semantics": "not-claimed",
            }
        ]
        report, summary = analyze_original_candidate_geometry_stream(original, candidate)
        self.assertEqual(len(report), 1)
        self.assertEqual(report[0]["symbol_exact"], "True")
        self.assertEqual(report[0]["triangle_count_exact"], "True")
        self.assertEqual(summary["joined_rows"], "1")
        self.assertEqual(summary["symbol_exact_rows"], "1")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_audio_event_asset_path_index_control_is_textual_and_negative(self) -> None:
        rows, summary = build_control(
            [
                {
                    "literal": "@AAUDIO/EVENTSPA.EVT",
                    "literal_occurrences": "1",
                    "xref_count": "0",
                    "matched_paths": "data/audio/eventspa.evt",
                },
                {
                    "literal": "@AUDIO/EVENTSPA.EVT",
                    "literal_occurrences": "1",
                    "xref_count": "0",
                    "matched_paths": "data/audio/eventspa.evt",
                },
                {
                    "literal": "AUDIO/EVENTSPA.EVT",
                    "literal_occurrences": "6",
                    "xref_count": "0",
                    "matched_paths": "data/audio/eventspa.evt",
                },
                {
                    "literal": "CAUDIO/EVENTSPA.EVT",
                    "literal_occurrences": "1",
                    "xref_count": "0",
                    "matched_paths": "data/audio/eventspa.evt",
                },
            ],
            {
                "event_path": "data/audio/eventspa.evt",
                "candidate_names": "goal,intro,tackle",
                "candidate_pairs": "1061",
                "status": "no-direct-selection-binding",
            },
        )
        self.assertEqual(len(rows), 1)
        self.assertEqual(summary["candidate_name_count"], "3")
        self.assertEqual(summary["xbe_literal_rows"], "4")
        self.assertEqual(summary["xbe_literal_occurrences"], "9")
        self.assertEqual(summary["matched_path_rows"], "4")
        self.assertEqual(summary["distinct_matched_paths"], "1")
        self.assertEqual(summary["status"], "verified-independent-audio-event-asset-path-index-control")
        self.assertEqual(summary["direct_selection_binding"], "not-proven")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_numeric_stream_ambiguity_control_does_not_promote_bounds_to_position(self) -> None:
        report, summary = analyze_numeric_stream_ambiguity(
            [
                {
                    "model_archive": "fixture.big",
                    "model_object": "unique.ord",
                    "validated_stream_count": "5",
                    "finite_float3_profile_count": "2",
                    "declared_bounds_containment_float3_count": "1",
                    "status": "verified-bounded-original-numeric-payload-structure",
                    "original_semantics": "not-claimed",
                },
                {
                    "model_archive": "fixture.big",
                    "model_object": "ambiguous.ord",
                    "validated_stream_count": "5",
                    "finite_float3_profile_count": "3",
                    "declared_bounds_containment_float3_count": "2",
                    "status": "verified-bounded-original-numeric-payload-structure",
                    "original_semantics": "not-claimed",
                },
            ]
        )
        self.assertEqual(len(report), 2)
        self.assertEqual(summary["valid_input_rows"], "2")
        self.assertEqual(summary["objects_with_unique_bounds_match"], "1")
        self.assertEqual(summary["objects_with_multiple_bounds_matches"], "1")
        self.assertEqual(summary["objects_with_non_unique_or_missing_match"], "1")
        self.assertEqual(summary["semantic_binding"], "not-proven")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_bounds_consistent_record_control_is_structural_only(self) -> None:
        rows = []
        for category, stride, bounds in (
            (10, "12", "True"),
            (11, "8", "False"),
            (12, "4", "False"),
            (13, "12", "False"),
            (14, "2", "False"),
        ):
            rows.append(
                {
                    "model_archive": "fixture.big",
                    "model_object": "fixture.ord",
                    "symbol_name": f"__VB:::__GPvb_{category}_0_fixture",
                    "category_key": str(category),
                    "ordinal_key": "0",
                    "opaque_key_valid": "True",
                    "stream_stride": stride,
                    "declared_bounds_containment_float3": bounds,
                    "status": "verified-bounded-original-numeric-stream-profile",
                    "original_semantics": "not-claimed",
                }
            )
        report, summary = analyze_bounds_consistent_record(rows)
        self.assertEqual(len(report), 1)
        self.assertEqual(summary["object_ordinal_groups"], "1")
        self.assertEqual(summary["complete_five_stream_groups"], "1")
        self.assertEqual(summary["first_raw_stride12_groups"], "1")
        self.assertEqual(summary["first_raw_bounds_match_groups"], "1")
        self.assertEqual(summary["later_raw_bounds_match_groups"], "0")
        self.assertEqual(summary["semantic_binding"], "not-proven")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_bounds_fetch_slot_join_preserves_opaque_slot_evidence(self) -> None:
        numeric_rows = []
        topology_rows = []
        for category, stream_stride, bounds, fetch_slot, fetch_stride in (
            (10, "12", "True", "0", "12"),
            (11, "8", "False", "1", "8"),
            (12, "4", "False", "2", "4"),
            (13, "12", "False", "3", "12"),
            (14, "2", "False", "4", "2"),
        ):
            numeric_rows.append(
                {
                    "model_archive": "fixture.big",
                    "model_object": "fixture.ord",
                    "symbol_name": f"__VB:::__GPvb_{category}_0_fixture",
                    "category_key": str(category),
                    "ordinal_key": "0",
                    "opaque_key_valid": "True",
                    "stream_stride": stream_stride,
                    "declared_bounds_containment_float3": bounds,
                    "status": "verified-bounded-original-numeric-stream-profile",
                    "original_semantics": "not-claimed",
                }
            )
            topology_rows.append(
                {
                    "model_archive": "fixture.big",
                    "model_object": "fixture.ord",
                    "primitive": "0",
                    "category": str(category),
                    "fetch_slot": fetch_slot,
                    "fetch_stride": fetch_stride,
                    "stream_stride": stream_stride,
                    "fetch_stride_matches_stream_stride": "True",
                    "valid_payload_range": "True",
                    "status": "verified-bounded-original-primitive-stream-topology",
                    "original_semantics": "not-claimed",
                }
            )
        report, summary = analyze_bounds_fetch_slot_join(numeric_rows, topology_rows)
        self.assertEqual(len(report), 5)
        self.assertEqual(summary["joined_rows"], "5")
        self.assertEqual(summary["object_ordinal_groups"], "1")
        self.assertEqual(summary["first_raw_fetch_slot_zero_groups"], "1")
        self.assertEqual(summary["first_raw_bounds_match_groups"], "1")
        self.assertEqual(summary["later_raw_bounds_match_groups"], "0")
        self.assertEqual(summary["semantic_binding"], "not-proven")

    def test_audio_parallel_runtime_join_keeps_event_sample_binding_unclaimed(self) -> None:
        parallel_rows = []
        runtime_rows = []
        for index in range(17):
            name = f"event_{index:02d}"
            parallel_rows.append(
                {
                    "event_name": name,
                    "header_row": str(index),
                    "data_offset": str(1000 + index),
                    "data_length": str(2000 + index),
                    "status": "verified-structural-named-parallel-range",
                    "original_semantics": "not-claimed",
                }
            )
            runtime_rows.append(
                {
                    "event_name": name,
                    "header_row": str(index),
                    "data_offset": str(1000 + index),
                    "data_length": str(2000 + index),
                    "candidate_metadata_validated": "True",
                    "runtime_prepared": "True",
                    "wave_header_valid": "True",
                    "status": "verified-runtime-named-payload",
                    "original_semantics": "not-claimed",
                }
            )
        report, summary = analyze_audio_parallel_runtime_join(parallel_rows, runtime_rows)
        self.assertEqual(len(report), 17)
        self.assertEqual(summary["joined_rows"], "17")
        self.assertEqual(summary["header_row_match_rows"], "17")
        self.assertEqual(summary["data_offset_match_rows"], "17")
        self.assertEqual(summary["data_length_match_rows"], "17")
        self.assertEqual(summary["runtime_prepared_rows"], "17")
        self.assertEqual(summary["status"], "verified-independent-audio-event-parallel-runtime-join-control")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_state_population_control_remains_observational(self) -> None:
        common = {
            "model_archive": "fixture.big",
            "model_object": "fixture.ord",
            "layer": "2",
            "command_count": "1",
            "command_bytes": "16",
            "command_stream_complete": "True",
            "command_bytes_sha256": "a" * 64,
            "status": "verified-bounded-original-render-chain-structure",
            "original_semantics": "not-claimed",
        }
        rows = [
            {
                **common,
                "primitive": "3",
                "has_shapename_assignment": "True",
                "has_primitive_type_assignment": "False",
            },
            {
                **common,
                "primitive": "4",
                "has_shapename_assignment": "False",
                "has_primitive_type_assignment": "True",
            },
        ]
        report_rows, summary = analyze_rows(rows)
        self.assertEqual(len(report_rows), 1)
        self.assertEqual(summary["population_overlap_rows"], "0")
        self.assertEqual(summary["same_primitive_index_pairs"], "0")
        self.assertEqual(summary["adjacent_primitive_pairs"], "1")
        self.assertEqual(summary["same_layer_pairs"], "1")
        self.assertEqual(summary["exact_command_hash_pairs"], "1")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_vertex_record_association_control_has_negative_controls(self) -> None:
        keys = "|".join(
            f"category={10 + index};ordinal=3;stride={stride};bytes=24;symbol=__VB:::__GPvb_{10 + index}_3_fixture"
            for index, stride in enumerate((12, 8, 4, 12, 2))
        )
        report_rows, summary = analyze_vertex_record_association_rows(
            [
                {
                    "model_archive": "fixture.big",
                    "model_object": "fixture.ord",
                    "layer": "0",
                    "primitive": "3",
                    "fetch_slot_strides": "12,8,4,12,2",
                    "record_keys": keys,
                    "unique_opaque_sequence": "True",
                }
            ]
        )
        self.assertEqual(len(report_rows), 1)
        self.assertEqual(summary["recomputed_exact_rows"], "1")
        self.assertEqual(summary["reverse_stride_match_rows"], "0")
        self.assertEqual(summary["shifted_ordinal_match_rows"], "0")
        self.assertEqual(
            summary["status"],
            "verified-independent-original-opaque-vertex-record-association-control",
        )
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_vertex_payload_control_reparses_numeric_signatures(self) -> None:
        report_rows, summary = analyze_vertex_payload_rows(
            [
                {
                    "model_archive": "fixture.big",
                    "model_object": "fixture.ord",
                    "validated_stream_count": "5",
                    "finite_packed_float_profile_count": "3",
                    "finite_float2_profile_count": "1",
                    "near_unit_range_float2_profile_count": "1",
                    "finite_float3_profile_count": "1",
                    "declared_bounds_containment_float3_count": "1",
                    "distinct_payload_range_count": "5",
                    "overlap_payload_pair_count": "0",
                    "numeric_signature": "streams=5;finite_packed=3;finite_stride8=1;finite_stride12=1;components=2x8=1,3x12=1;bounds_matching_stride12=1",
                    "status": "verified-bounded-original-numeric-payload-structure",
                    "original_semantics": "not-claimed",
                }
            ]
        )
        self.assertEqual(len(report_rows), 1)
        self.assertEqual(summary["numeric_signature_match_rows"], "1")
        self.assertEqual(summary["payload_range_metadata_valid_rows"], "1")
        self.assertEqual(summary["input_status_valid_rows"], "1")
        self.assertEqual(summary["status"], "verified-independent-original-numeric-payload-control")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_state_token_control_keeps_literal_vocabulary_negative(self) -> None:
        common = {
            "model_archive": "fixture.big",
            "model_object": "fixture.ord",
            "layer": "0",
            "assignment_count": "4",
            "has_shapename_assignment": "True",
            "has_clamp_u_assignment": "True",
            "has_clamp_v_assignment": "True",
            "has_primitive_type_assignment": "False",
            "input_status": "verified-bounded-original-render-chain-structure",
        }
        report_rows, summary = analyze_state_token_rows(
            [
                {
                    **common,
                    "primitive": "0",
                    "assignment_names": "RUNTIME_ALLOC::UID,SHAPENAME,XBOXEXTOBJ_SetClampU,XBOXEXTOBJ_SetClampV",
                },
                {
                    **common,
                    "primitive": "1",
                    "assignment_count": "2",
                    "assignment_names": "RUNTIME_ALLOC::UID,SetPrimitiveType",
                    "has_shapename_assignment": "False",
                    "has_clamp_u_assignment": "False",
                    "has_clamp_v_assignment": "False",
                    "has_primitive_type_assignment": "True",
                },
            ]
        )
        self.assertEqual(len(report_rows), 2)
        self.assertEqual(summary["assignment_name_reparse_match_rows"], "2")
        self.assertEqual(summary["classification_flags_match_rows"], "2")
        self.assertEqual(summary["negative_token_counts_zero_rows"], "2")
        self.assertEqual(summary["texture_token_rows"], "0")
        self.assertEqual(summary["material_token_rows"], "0")
        self.assertEqual(summary["uv_coordinate_token_rows"], "0")
        self.assertEqual(summary["vertex_attribute_token_rows"], "0")
        self.assertEqual(summary["deformation_token_rows"], "0")
        self.assertEqual(summary["status"], "verified-independent-original-state-token-control")
        self.assertEqual(summary["original_semantics"], "not-claimed")

        token_rows, token_summary = analyze_state_token_rows(
            [
                {
                    **common,
                    "primitive": "2",
                    "assignment_names": "SetTexture",
                    "assignment_count": "1",
                    "has_shapename_assignment": "False",
                    "has_clamp_u_assignment": "False",
                    "has_clamp_v_assignment": "False",
                    "has_primitive_type_assignment": "False",
                }
            ]
        )
        self.assertEqual(len(token_rows), 1)
        self.assertEqual(token_summary["texture_token_rows"], "1")
        self.assertEqual(token_summary["status"], "inconclusive-original-state-token-control")

    def test_original_render_payload_object_join_stays_object_level(self) -> None:
        render_rows = [
            {
                "model_archive": "fixture.big",
                "model_object": "fixture.ord",
                "primitive": "0",
                "command_count": "2",
                "command_bytes": "32",
                "command_stream_complete": "True",
                "state_relocation_present": "True",
                "fetch_slot_count": "5",
                "status": "verified-bounded-original-render-chain-structure",
                "original_semantics": "not-claimed",
            },
            {
                "model_archive": "fixture.big",
                "model_object": "fixture.ord",
                "primitive": "1",
                "command_count": "3",
                "command_bytes": "48",
                "command_stream_complete": "True",
                "state_relocation_present": "True",
                "fetch_slot_count": "5",
                "status": "verified-bounded-original-render-chain-structure",
                "original_semantics": "not-claimed",
            },
        ]
        payload_rows = [
            {
                "model_archive": "fixture.big",
                "model_object": "fixture.ord",
                "validated_stream_count": "5",
                "finite_packed_float_profile_count": "3",
                "finite_float2_profile_count": "1",
                "declared_bounds_containment_float3_count": "1",
                "distinct_payload_range_count": "5",
                "overlap_payload_pair_count": "0",
                "status": "verified-bounded-original-numeric-payload-structure",
                "original_semantics": "not-claimed",
            }
        ]
        report_rows, summary = analyze_render_payload_object_join_rows(render_rows, payload_rows)
        self.assertEqual(len(report_rows), 1)
        self.assertEqual(summary["joined_object_rows"], "1")
        self.assertEqual(summary["render_primitive_rows"], "2")
        self.assertEqual(summary["payload_validated_stream_rows"], "5")
        self.assertEqual(summary["joined_identity_match_rows"], "1")
        self.assertEqual(summary["reversed_key_match_rows"], "0")
        self.assertEqual(summary["shifted_key_match_rows"], "0")
        self.assertEqual(summary["status"], "verified-independent-original-render-payload-object-join")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_primitive_stream_topology_preserves_shifted_ordinal_ambiguity(self) -> None:
        rows = []
        for primitive, ordinal in ((0, 0), (1, 1)):
            for index, (category, stride) in enumerate(zip((10, 11, 12, 13, 14), (12, 8, 4, 12, 2))):
                rows.append(
                    {
                        "model_archive": "fixture.big",
                        "model_object": "fixture.ord",
                        "layer": "0",
                        "primitive": str(primitive),
                        "fetch_slot": str(index),
                        "fetch_stride": str(stride),
                        "category": str(category),
                        "ordinal": str(ordinal),
                        "symbol_name": f"__VB:::__GPvb_{category}_{ordinal}_fixture",
                        "stream_stride": str(stride),
                        "element_count": "3",
                        "category_sequence_valid": "True",
                        "key_ordinal_matches_primitive": "True",
                        "fetch_slot_structure_valid": "True",
                        "fetch_stride_matches_stream_stride": "True",
                        "valid_payload_range": "True",
                        "structural_match": "True",
                        "status": "verified-bounded-original-primitive-stream-topology",
                        "original_semantics": "not-claimed",
                    }
                )
        report_rows, summary = analyze_primitive_stream_topology_rows(rows)
        self.assertEqual(len(report_rows), 2)
        self.assertEqual(summary["groups_with_structural_match"], "2")
        self.assertEqual(summary["reversed_slot_match_rows"], "0")
        self.assertEqual(summary["shifted_ordinal_match_rows"], "1")
        self.assertEqual(
            summary["status"],
            "verified-independent-original-primitive-stream-topology-control",
        )
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_primitive_stream_opaque_discrimination_preserves_negative_overlap(self) -> None:
        rows = []
        for primitive, candidates in ((0, "0,1"), (1, "1")):
            rows.append(
                {
                    "model_archive": "fixture.big",
                    "model_object": "fixture.ord",
                    "layer": "0",
                    "primitive": str(primitive),
                    "fetch_word_count": "5",
                    "candidate_ordinal_count": str(len(candidates.split(","))),
                    "stride_matching_candidate_count": str(len(candidates.split(","))),
                    "metadata_matching_candidate_count": "0",
                    "payload_prefix_matching_candidate_count": "0",
                    "stride_matching_ordinals": candidates,
                    "metadata_matching_ordinals": "",
                    "payload_prefix_matching_ordinals": "",
                    "current_ordinal_present": "True",
                    "current_ordinal_metadata_match": "False",
                    "current_ordinal_payload_prefix_match": "False",
                    "current_ordinal_uniquely_discriminated_by_metadata": "False",
                    "current_ordinal_uniquely_discriminated_by_payload_prefix": "False",
                    "status": "verified-bounded-original-primitive-stream-opaque-discrimination",
                    "original_semantics": "not-claimed",
                }
            )
        report_rows, summary = analyze_primitive_stream_opaque_discrimination_rows(rows)
        self.assertEqual(len(report_rows), 2)
        self.assertEqual(summary["candidate_ordinal_rows"], "3")
        self.assertEqual(summary["metadata_matching_candidate_rows"], "0")
        self.assertEqual(summary["payload_prefix_matching_candidate_rows"], "0")
        self.assertEqual(summary["shifted_stride_candidate_rows"], "1")
        self.assertEqual(
            summary["status"],
            "verified-independent-original-primitive-stream-opaque-discrimination-control",
        )
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_primitive_stream_command_descriptor_adjacency_is_structural(self) -> None:
        render_rows = [
            {
                "model_archive": "fixture.big",
                "model_object": "fixture.ord",
                "layer": "0",
                "primitive": "0",
                "command_section_index": "2",
                "command_stream_offset": "0x20c",
                "command_stream_complete": "True",
                "status": "verified-bounded-original-render-chain-structure",
                "original_semantics": "not-claimed",
            }
        ]
        topology_rows = []
        for ordinal, base in ((0, 0x180), (1, 0x300)):
            for category in range(10, 15):
                topology_rows.append(
                    {
                        "model_archive": "fixture.big",
                        "model_object": "fixture.ord",
                        "layer": "0",
                        "primitive": str(ordinal),
                        "section_index": "2",
                        "descriptor_offset": f"0x{base + (category - 10) * 0x20:x}",
                        "category": str(category),
                        "ordinal": str(ordinal),
                        "stream_stride": "2",
                        "symbol_name": f"__VB:::__GPvb_{category}_{ordinal}_fixture",
                        "status": "verified-bounded-original-primitive-stream-topology",
                        "original_semantics": "not-claimed",
                    }
                )
        report_rows, summary = analyze_primitive_stream_command_descriptor_adjacency(
            render_rows,
            topology_rows,
        )
        self.assertEqual(len(report_rows), 1)
        self.assertEqual(summary["candidate_ordinal_rows"], "2")
        self.assertEqual(summary["identity_candidate_rows"], "1")
        self.assertEqual(summary["shifted_ordinal_identity_rows"], "0")
        self.assertEqual(summary["exact_adjacency_stream_rows"], "1")
        self.assertEqual(summary["exact_adjacency_category_histogram"], "14:1")
        self.assertEqual(summary["exact_adjacency_stride_histogram"], "2:1")
        self.assertEqual(report_rows[0]["exact_adjacency_stream_categories"], "0:14")
        self.assertEqual(report_rows[0]["exact_adjacency_stream_strides"], "0:2")
        self.assertEqual(
            summary["status"],
            "verified-independent-original-primitive-stream-command-descriptor-adjacency-control",
        )
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_state_fetch_adjacency_control_is_observational(self) -> None:
        render_rows = [
            {
                "model_archive": "fixture.big",
                "model_object": "fixture.ord",
                "layer": "0",
                "primitive": "0",
                "state_relocation_present": "True",
                "has_shapename_assignment": "True",
                "has_primitive_type_assignment": "False",
                "complete_five_slot_fetch_sequence": "True",
                "state_fetch_overlap": "True",
                "state_shapename_primitive_type_overlap": "False",
                "status": "verified-bounded-original-render-chain-structure",
                "original_semantics": "not-claimed",
            }
        ]
        adjacency_rows = [
            {
                "model_archive": "fixture.big",
                "model_object": "fixture.ord",
                "layer": "0",
                "primitive": "0",
                "exact_adjacency_stream_count": "1",
                "exact_adjacency_stream_categories": "0:14",
                "exact_adjacency_stream_strides": "0:2",
                "render_input_status": "verified-bounded-original-render-chain-structure",
                "status": "verified-bounded-original-primitive-stream-command-descriptor-adjacency",
                "original_semantics": "not-claimed",
            }
        ]
        report_rows, summary = analyze_state_fetch_adjacency(
            render_rows,
            adjacency_rows,
        )
        self.assertEqual(len(report_rows), 1)
        self.assertEqual(summary["joined_rows"], "1")
        self.assertEqual(summary["joined_state_fetch_exact_adjacency_rows"], "1")
        self.assertEqual(summary["exact_adjacency_category_histogram"], "14:1")
        self.assertEqual(
            summary["status"],
            "verified-independent-original-state-fetch-adjacency-control",
        )
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_primitive_fetch_topology_control_preserves_incomplete_slots(self) -> None:
        render_rows = [
            {
                "model_archive": "fixture.big",
                "model_object": "fixture.ord",
                "layer": "0",
                "primitive": "0",
                "fetch_slot_count": "4",
                "fetch_slot_ids": "0,1,3,4",
                "complete_five_slot_fetch_sequence": "False",
                "status": "verified-bounded-original-render-chain-structure",
                "original_semantics": "not-claimed",
            }
        ]
        topology_rows = []
        for fetch_slot in (0, 1, 3, 4, -1):
            topology_rows.append(
                {
                    "model_archive": "fixture.big",
                    "model_object": "fixture.ord",
                    "layer": "0",
                    "primitive": "0",
                    "fetch_slot": str(fetch_slot),
                    "key_ordinal_matches_primitive": "True",
                    "valid_payload_range": "True",
                    "structural_match": str(fetch_slot >= 0),
                    "status": (
                        "verified-bounded-original-primitive-stream-topology"
                        if fetch_slot >= 0
                        else "inconclusive-original-primitive-stream-topology"
                    ),
                    "original_semantics": "not-claimed",
                }
            )
        report_rows, summary = analyze_primitive_fetch_topology_rows(
            render_rows,
            topology_rows,
        )
        self.assertEqual(len(report_rows), 1)
        self.assertEqual(summary["joined_rows"], "1")
        self.assertEqual(summary["topology_valid_slot_rows"], "4")
        self.assertEqual(summary["fetch_slot_id_set_match_rows"], "1")
        self.assertEqual(summary["complete_five_slot_sequence_agreement_rows"], "1")
        self.assertEqual(summary["incomplete_slot_two_rows"], "1")
        self.assertEqual(
            summary["status"],
            "verified-independent-original-primitive-fetch-topology-control",
        )
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_original_command_payload_region_control_preserves_raw_gap(self) -> None:
        render_rows = [{
            "model_archive": "fixture.big",
            "model_object": "fixture.ord",
            "layer": "0",
            "primitive": "0",
            "command_section_index": "1",
            "command_stream_offset": "0xD0",
            "command_bytes": "0x20",
            "fetch_slot_ids": "0,1,3,4",
            "status": "verified-bounded-original-render-chain-structure",
            "original_semantics": "not-claimed",
        }]
        topology_rows = [
            {
                "model_archive": "fixture.big",
                "model_object": "fixture.ord",
                "layer": "0",
                "primitive": "0",
                "section_index": "1",
                "data_offset": hex(offset),
                "byte_length": hex(length),
                "valid_payload_range": "True",
                "status": "inconclusive-original-primitive-stream-topology",
                "original_semantics": "not-claimed",
            }
            for offset, length in ((0x20, 0x20), (0x40, 0x20), (0x60, 0x20), (0x80, 0x20), (0xA0, 0x20))
        ]
        report_rows, summary = analyze_command_payload_region_rows(
            render_rows,
            topology_rows,
        )
        self.assertEqual(len(report_rows), 1)
        self.assertEqual(summary["joined_rows"], "1")
        self.assertEqual(summary["payload_interval_nonoverlap_rows"], "1")
        self.assertEqual(summary["command_payload_nonoverlap_rows"], "1")
        self.assertEqual(summary["command_after_payload_end_rows"], "1")
        self.assertEqual(summary["command_payload_gap_equals_0x10_rows"], "1")
        self.assertEqual(summary["command_payload_gap_histogram"], "16:1")
        self.assertEqual(summary["incomplete_slot_two_rows"], "1")
        self.assertEqual(
            summary["status"],
            "verified-independent-original-command-payload-region-control",
        )
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_render_indirect_indexed_accessor_shapes_are_opaque(self) -> None:
        self.assertEqual(
            _shape(["pop", "pop", "ret"]),
            "fallback-pop-pop-ret",
        )
        self.assertEqual(
            _shape(["mov", "pop", "pop", "ret"]),
            "single-mov-memory-ret",
        )
        self.assertEqual(
            _shape(["lea", "lea", "push", "push", "call", "pop", "pop", "ret"]),
            "lea-lea-push-push-call-ret",
        )
        self.assertEqual(_shape(["mov", "call", "ret"]), "other")

    def test_retail_xbe_structure(self) -> None:
        result = analyze_xbe(ISO)
        self.assertEqual(result["magic"], "XBEH")
        self.assertEqual(result["decoded_entry_points"]["retail_candidate"], 0x0024D8BE)
        self.assertTrue(result["entry_point_in_executable_section"]["retail_candidate"])
        self.assertEqual(len(result["sections"]), 11)
        self.assertTrue(all(section["sha1_matches_file_data"] for section in result["sections"]))
        self.assertEqual(len(result["kernel_thunks"]["retail_candidate"]["ordinals"]), 128)

    def test_expected_asset_signatures(self) -> None:
        reader = XdvdfsReader(ISO)
        try:
            entries = {entry.path: entry for entry in reader.entries()}
            for path, magic in {
                "data/audio/AMBSTRM.abk": b"ABKC",
                "data/audio/AMBSTRM.ast": b"SCHl",
                "data/aptanims/player0.xsh": b"SHPX",
                "data/database/db.bh": b"VivF",
            }.items():
                entry = entries[path]
                self.assertIsNotNone(entry.image_offset)
                self.assertEqual(reader._read_at(entry.image_offset, 4), magic)
        finally:
            reader.close()

    def test_big_indices(self) -> None:
        reports = {report["path"]: report for report in inspect_big_archives(ISO)}
        self.assertEqual(reports["data/anims.big"]["status"], "OK")
        self.assertEqual(reports["data/anims.big"]["file_count"], 58)
        self.assertEqual(reports["data/audio/loadloop.big"]["status"], "OK")
        self.assertEqual(reports["data/audio/loadloop.big"]["file_count"], 58)

    def test_xbe_string_xref_literal_triage(self) -> None:
        self.assertEqual(literal_category("AUDIO/AMBSTRM.ABK"), "asset-extension-literal")
        self.assertEqual(literal_category("D3DTexture"), "api-or-runtime-token-literal")
        self.assertIsNone(literal_category("ordinary text"))
        sections = [
            {
                "index": 5,
                "name": ".rdata",
                "executable": True,
                "file_address": 0,
                "file_size": 18,
                "virtual_address": 0x2A9020,
            }
        ]
        literals = collect_literals(b"AUDIO/AMBSTRM.ABK\x00", sections)
        self.assertEqual(len(literals), 1)
        self.assertEqual(literals[0]["virtual_address"], 0x2A9020)
        self.assertEqual(literals[0]["category"], "asset-extension-literal")

    def test_xbe_string_xref_call_context_is_local_only(self) -> None:
        xrefs = [
            {
                "source_section": ".text",
                "source_virtual_address": 0x1100,
                "string_virtual_address": 0x2A9020,
                "string": "AUDIO/AMBSTRM.ABK",
                "literal_category": "asset-extension-literal",
            }
        ]
        calls = [
            {
                "source_section": ".text",
                "source_virtual_address": 0x10F0,
                "source_file_offset": 0xF0,
                "instruction_bytes": "e8",
                "target_virtual_address": 0x1200,
                "target_is_executable": True,
            },
            {
                "source_section": ".text",
                "source_virtual_address": 0x1300,
                "source_file_offset": 0x300,
                "instruction_bytes": "e8",
                "target_virtual_address": 0x1400,
                "target_is_executable": True,
            },
        ]
        context = build_call_context(xrefs, calls, window=32)
        self.assertEqual(len(context), 1)
        self.assertEqual(context[0]["call_target_virtual_address"], 0x1200)
        self.assertEqual(context[0]["distance_bytes"], 16)
        self.assertEqual(context[0]["status"], "candidate-local-x86-call-context")
        self.assertEqual(context[0]["original_semantics"], "not-claimed")

    def test_xbe_string_xref_call_clusters_are_cooccurrence_only(self) -> None:
        context = [
            {
                "call_target_virtual_address": 0x1200,
                "target_is_executable": True,
                "distance_bytes": 8,
                "xref_source_virtual_address": 0x1100,
                "xref_string": ".xsh",
                "literal_category": "asset-extension-literal",
            },
            {
                "call_target_virtual_address": 0x1200,
                "target_is_executable": True,
                "distance_bytes": 16,
                "xref_source_virtual_address": 0x1120,
                "xref_string": "SHAPENAME",
                "literal_category": "subsystem-token-literal",
            },
            {
                "call_target_virtual_address": 0x1300,
                "target_is_executable": False,
                "distance_bytes": 4,
                "xref_source_virtual_address": 0x1130,
                "xref_string": "ignored",
                "literal_category": "content-or-path-literal",
            },
        ]
        clusters = build_call_clusters(context)
        self.assertEqual(len(clusters), 1)
        self.assertEqual(clusters[0]["call_target_virtual_address"], 0x1200)
        self.assertEqual(clusters[0]["unique_xref_strings"], 2)
        self.assertEqual(clusters[0]["min_distance_bytes"], 8)
        self.assertEqual(clusters[0]["status"], "candidate-local-call-target-cooccurrence")
        self.assertEqual(clusters[0]["original_semantics"], "not-claimed")

    def test_xbe_asset_literal_crosscheck_is_textual_only(self) -> None:
        self.assertEqual(normalize_path(r"AUDIO\\AMBSTRM.ABK"), "audio/ambstrm.abk")
        self.assertTrue(is_asset_literal("AUDIO/AMBSTRM.ABK"))
        self.assertTrue(is_asset_literal("models/skel0000.ord"))
        self.assertFalse(is_asset_literal("AnimationBank"))

        kind, matches = match_literal_to_paths(
            "AUDIO/AMBSTRM.ABK",
            ["data/audio/AMBSTRM.abk", "data/audio/PLAYER.abk"],
        )
        self.assertEqual(kind, "data-prefix-xdvdfs-path")
        self.assertEqual(matches, ["data/audio/AMBSTRM.abk"])

        ambiguous_kind, ambiguous = match_literal_to_paths(
            "foo.big",
            ["data/a/foo.big", "data/b/foo.big"],
        )
        self.assertEqual(ambiguous_kind, "ambiguous-xdvdfs-suffix")
        self.assertEqual(ambiguous, ["data/a/foo.big", "data/b/foo.big"])

    def test_nested_big_literal_crosscheck_is_textual_only(self) -> None:
        refpack = bytes.fromhex("10fb000008e14142434445464748fc")
        self.assertEqual(decode_refpack(refpack), b"ABCDEFGH")
        big = bytearray(36)
        big[0:4] = b"BIGF"
        big[4:8] = (36).to_bytes(4, "little")
        big[8:12] = (1).to_bytes(4, "big")
        big[12:16] = (29).to_bytes(4, "big")
        big[16:20] = (29).to_bytes(4, "big")
        big[20:24] = (7).to_bytes(4, "big")
        big[24:28] = b"mesh"
        big[28] = 0
        self.assertEqual(parse_big_index(bytes(big))[0]["name"], "mesh")
        kind, matches = match_literal_to_nested_paths(
            "models/skel0000.ord",
            ["data/models.big!star0001.big!skel0000.ord"],
        )
        self.assertEqual(kind, "unique-big-entry-basename")
        self.assertEqual(matches, ["data/models.big!star0001.big!skel0000.ord"])

    def test_nested_literal_xref_join_stays_static(self) -> None:
        rows = join_rows(
            [{
                "image": "image.iso",
                "internal_path": "default.xbe",
                "xbe_sha256": "xbe",
                "xbe_size": "1",
                "literal": "models/skel0000.ord",
                "literal_category": "asset-extension-literal",
                "literal_occurrences": "1",
                "match_kind": "unique-big-entry-suffix",
                "matched_nested_entries": "data/models.big!skel0000.ord",
            }],
            [{"string": "models/skel0000.ord", "source_virtual_address": "0x1000"}],
            [{
                "xref_string": "models/skel0000.ord",
                "call_source_virtual_address": "0x1010",
                "call_target_virtual_address": "0x2000",
                "target_is_executable": "True",
            }],
        )
        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0]["xbe_xref_count"], "1")
        self.assertEqual(rows[0]["nearby_executable_call_targets"], "1")
        self.assertEqual(rows[0]["original_semantics"], "not-claimed")

    def test_audio_static_cluster_audit_stays_static(self) -> None:
        self.assertTrue(is_audio_literal("AUDIO/AMBSTRM.ABK"))
        self.assertTrue(is_audio_literal("BeatAudio"))
        self.assertFalse(is_audio_literal("stadium.big"))
        xrefs = [
            {
                "string": "AUDIO/AMBSTRM.ABK",
                "literal_category": "asset-extension-literal",
                "source_virtual_address": "0x1000",
                "xbe_sha256": "xbe",
                "xbe_size": "10",
                "image": "image.iso",
                "internal_path": "default.xbe",
            },
            {
                "string": "BeatAudio",
                "literal_category": "subsystem-token-literal",
                "source_virtual_address": "0x1100",
                "xbe_sha256": "xbe",
                "xbe_size": "10",
                "image": "image.iso",
                "internal_path": "default.xbe",
            },
        ]
        context = [
            {
                "xref_string": "AUDIO/AMBSTRM.ABK",
                "xref_source_virtual_address": "0x1000",
                "call_target_virtual_address": "0x2000",
                "call_source_virtual_address": "0x1010",
                "target_is_executable": "True",
                "distance_bytes": "16",
                "literal_category": "asset-extension-literal",
            },
            {
                "xref_string": "BeatAudio",
                "xref_source_virtual_address": "0x1100",
                "call_target_virtual_address": "0x2000",
                "call_source_virtual_address": "0x1110",
                "target_is_executable": "True",
                "distance_bytes": "16",
                "literal_category": "subsystem-token-literal",
            },
        ]
        literal_rows = build_audio_literal_rows(
            xrefs,
            context,
            [{
                "literal": "AUDIO/AMBSTRM.ABK",
                "match_kind": "data-prefix-xdvdfs-path",
                "matched_nested_entries": "data/audio/AMBSTRM.abk",
            }],
            [],
        )
        clusters = build_audio_clusters(context)
        self.assertEqual(len(literal_rows), 2)
        self.assertEqual(literal_rows[0]["nested_match_kind"], "data-prefix-xdvdfs-path")
        self.assertEqual(len(clusters), 1)
        self.assertEqual(clusters[0]["unique_audio_literals"], "2")
        self.assertEqual(clusters[0]["original_semantics"], "not-claimed")

    def test_nested_payload_inventory_classifies_headers_only(self) -> None:
        self.assertEqual(_format_from_header(b"\x7fELF"), "elf32-little-endian-candidate")
        self.assertEqual(_format_from_header(b"BILG\x00"), "bilg-candidate")
        self.assertEqual(_format_from_header(b"\x00.symtab\x00.strtab"), "elf-companion-sidecar-candidate")
        self.assertEqual(_format_from_header(b"stadium payload"), "printable-payload-candidate")

    def test_render_static_cluster_audit_stays_static(self) -> None:
        self.assertTrue(is_render_literal("D3DTexture"))
        self.assertTrue(is_render_literal("models/skel0000.ord"))
        self.assertTrue(is_render_literal("cRenderProperties"))
        self.assertFalse(is_render_literal("ordinary gameplay text"))
        self.assertIn("d3d-or-render-api-token", classify_render_literal("D3DTexture"))
        xrefs = [
            {
                "string": "D3DTexture",
                "literal_category": "api-or-runtime-token-literal",
                "source_virtual_address": "0x1000",
                "xbe_sha256": "xbe",
                "xbe_size": "10",
                "image": "image.iso",
                "internal_path": "default.xbe",
            },
            {
                "string": "models/skel0000.ord",
                "literal_category": "asset-extension-literal",
                "source_virtual_address": "0x1100",
                "xbe_sha256": "xbe",
                "xbe_size": "10",
                "image": "image.iso",
                "internal_path": "default.xbe",
            },
        ]
        context = [
            {
                "xref_string": "D3DTexture",
                "xref_source_virtual_address": "0x1000",
                "call_target_virtual_address": "0x2000",
                "call_source_virtual_address": "0x1010",
                "target_is_executable": "True",
                "distance_bytes": "16",
                "literal_category": "api-or-runtime-token-literal",
            },
            {
                "xref_string": "models/skel0000.ord",
                "xref_source_virtual_address": "0x1100",
                "call_target_virtual_address": "0x2000",
                "call_source_virtual_address": "0x1110",
                "target_is_executable": "True",
                "distance_bytes": "32",
                "literal_category": "asset-extension-literal",
            },
        ]
        rows = build_render_literal_rows(xrefs, context)
        clusters = build_render_clusters(context)
        self.assertEqual(len(rows), 2)
        self.assertEqual(len(clusters), 1)
        self.assertEqual(clusters[0]["unique_render_literals"], "2")
        self.assertEqual(clusters[0]["status"], "candidate-static-render-call-target-cooccurrence")
        self.assertEqual(clusters[0]["original_semantics"], "not-claimed")

    def test_render_target_selection_is_bounded_and_stable(self) -> None:
        rows = [
            {"call_target_virtual_address": "0x1200", "unique_render_literals": "4"},
            {"call_target_virtual_address": "0x1300", "unique_render_literals": "3"},
            {"call_target_virtual_address": "0x1200", "unique_render_literals": "4"},
        ]
        selected = select_targets(rows, 2)
        self.assertEqual([row["call_target_virtual_address"] for row in selected], ["0x1200", "0x1300"])

    def test_render_cfg_reachability_is_bounded_and_does_not_follow_calls(self) -> None:
        from capstone import CS_ARCH_X86, CS_MODE_32, Cs

        decoder = Cs(CS_ARCH_X86, CS_MODE_32)
        decoder.detail = True
        instructions = list(decoder.disasm(bytes.fromhex("55 8b ec 75 01 90 c3"), 0x1000))
        cfg = recover_cfg(instructions, 0x1000)
        self.assertEqual(len(cfg["reachable"]), 5)
        self.assertEqual(cfg["conditional_branch_count"], 1)
        self.assertEqual(cfg["internal_branch_targets"], {0x1006})
        self.assertEqual(cfg["reachable_ret_count"], 1)
        self.assertEqual(cfg["out_of_window_branch_targets"], 0)

    def test_render_call_target_aggregation_is_one_hop(self) -> None:
        from capstone import CS_ARCH_X86, CS_MODE_32, Cs

        decoder = Cs(CS_ARCH_X86, CS_MODE_32)
        decoder.detail = True
        data = bytes.fromhex("e8 01 00 00 00 90 c3")
        result = {
            "fields": {"base_address": 0x1000},
            "sections": [
                {
                    "name": ".text",
                    "executable": True,
                    "virtual_address": 0x1000,
                    "virtual_size": len(data),
                    "file_address": 0,
                    "file_size": len(data),
                }
            ],
        }
        targets, windows = aggregate_call_targets(
            data,
            result,
            [{"target_virtual_address": "0x1000"}],
            len(data),
        )
        self.assertEqual(windows, 1)
        self.assertEqual(sorted(targets), [0x1006])
        self.assertEqual(targets[0x1006]["source_addresses"], {0x1000})

    def test_render_call_graph_is_bounded_and_direct_only(self) -> None:
        data = bytes.fromhex("e8 00 00 00 00 e8 00 00 00 00 c3")
        result = {
            "fields": {"base_address": 0x1000},
            "sections": [
                {
                    "name": ".text",
                    "executable": True,
                    "virtual_address": 0x1000,
                    "virtual_size": len(data),
                    "file_address": 0,
                    "file_size": len(data),
                }
            ],
        }
        nodes, edges, truncated = build_graph(
            data,
            result,
            [{"call_target_virtual_address": "0x1005"}],
            max_depth=2,
            max_bytes=len(data),
            max_nodes=10,
        )
        self.assertFalse(truncated)
        self.assertEqual(sorted(nodes), [0x1005, 0x100A])
        self.assertEqual(sorted(edges), [(0x1005, 0x100A)])
        self.assertEqual(nodes[0x100A]["reachable_ret_count"], 1)

    def test_render_indirect_transfer_inventory_preserves_operands(self) -> None:
        from capstone import CS_ARCH_X86, CS_MODE_32, Cs

        decoder = Cs(CS_ARCH_X86, CS_MODE_32)
        decoder.detail = True
        instructions = list(
            decoder.disasm(
                bytes.fromhex("ff 12 ff d0 ff 24 95 00 10 00 00"),
                0x1000,
            )
        )
        rows = classify_indirect_transfers(instructions, {0, 1, 2})
        self.assertEqual(len(rows), 3)
        self.assertEqual([row["operand_kind"] for row in rows], ["memory", "register", "memory"])
        self.assertEqual(rows[0]["addressing_mode"], "base")
        self.assertEqual(rows[1]["addressing_mode"], "register")
        self.assertEqual(rows[2]["addressing_mode"], "index")
        self.assertEqual(rows[2]["memory_scale"], "4")
        self.assertEqual(rows[2]["memory_displacement"], "0x1000")

    def test_render_indirect_reference_audit_reads_only_bounded_words(self) -> None:
        self.assertEqual(
            reference_address(
                {
                    "addressing_mode": "absolute-memory",
                    "memory_base_register": "",
                    "memory_index_register": "",
                    "memory_displacement": "0x1004",
                }
            ),
            (0x1004, "absolute-memory-word"),
        )
        self.assertEqual(
            reference_address(
                {
                    "addressing_mode": "index",
                    "memory_base_register": "",
                    "memory_index_register": "edx",
                    "memory_displacement": "0x1008",
                }
            ),
            (0x1008, "indexed-table-base-first-word"),
        )
        self.assertEqual(
            reference_address(
                {
                    "addressing_mode": "base",
                    "memory_base_register": "eax",
                    "memory_index_register": "",
                    "memory_displacement": "0x8",
                }
            ),
            (None, "not-applicable-nonabsolute"),
        )
        result = {
            "fields": {"base_address": 0x1000},
            "sections": [
                {
                    "name": ".text",
                    "executable": True,
                    "virtual_address": 0x1000,
                    "virtual_size": 0x20,
                    "file_address": 0,
                    "file_size": 0x20,
                }
            ],
        }
        word, offset, section = read_reference_word(bytes.fromhex("00100000 78563412"), result, 0x1004)
        self.assertEqual(word, 0x12345678)
        self.assertEqual(offset, 4)
        self.assertEqual(section["name"], ".text")

    def test_render_indirect_reference_slice_audit_groups_raw_words_without_binding(self) -> None:
        rows = [
            {
                "raw_word": "0x1000",
                "memory_reference_virtual_address": "0x2000",
                "instruction_virtual_address": "0x3000",
            },
            {
                "raw_word": "0x1000",
                "memory_reference_virtual_address": "0x2004",
                "instruction_virtual_address": "0x3004",
            },
            {
                "raw_word": "0x1100",
                "memory_reference_virtual_address": "0x2010",
                "instruction_virtual_address": "0x3010",
            },
        ]
        words = collect_reference_words(rows)
        self.assertEqual(sorted(words), [0x1000, 0x1100])
        self.assertEqual(words[0x1000]["reference_row_count"], 2)
        self.assertEqual(words[0x1000]["reference_addresses"], {0x2000, 0x2004})
        self.assertEqual(words[0x1000]["source_instruction_addresses"], {0x3000, 0x3004})

    def test_render_indirect_window_join_is_exact_and_static(self) -> None:
        targets = [
            {"target_virtual_address": "0x1000", "context_rows": "2"},
            {"target_virtual_address": "0x2000", "context_rows": "1"},
        ]
        transfers = [
            {
                "source_target_virtual_address": "0x1000",
                "instruction_virtual_address": "0x1010",
                "mnemonic": "call",
            },
            {
                "source_target_virtual_address": "0x1000",
                "instruction_virtual_address": "0x1020",
                "mnemonic": "jmp",
            },
        ]
        references = [
            {
                "source_target_virtual_address": "0x1000",
                "instruction_virtual_address": "0x1010",
                "raw_word": "0x00002000",
            },
            {
                "source_target_virtual_address": "0x1000",
                "instruction_virtual_address": "0x1010",
                "raw_word": "0x00002000",
            },
        ]
        slices = [
            {
                "candidate_target_virtual_address": "0x00002000",
                "ret_found": "True",
                "prologue_shape": "none",
            }
        ]

        rows, summary = build_join(targets, transfers, references, slices)

        self.assertEqual(summary["target_windows"], "2")
        self.assertEqual(summary["targets_with_indirect_transfers"], "1")
        self.assertEqual(summary["total_indirect_transfers"], "2")
        self.assertEqual(summary["indirect_calls"], "1")
        self.assertEqual(summary["indirect_jumps"], "1")
        self.assertEqual(summary["targets_with_references"], "1")
        self.assertEqual(summary["reference_rows"], "2")
        self.assertEqual(summary["targets_with_transfer_reference_instruction_overlap"], "1")
        self.assertEqual(summary["exact_transfer_reference_instruction_rows"], "2")
        self.assertEqual(summary["distinct_raw_words"], "1")
        self.assertEqual(summary["executable_raw_words"], "1")
        self.assertEqual(summary["disassembled_candidate_words"], "1")
        self.assertEqual(summary["ret_terminated_candidate_words"], "1")
        self.assertEqual(summary["prologue_like_candidate_words"], "0")
        self.assertEqual(summary["status"], "verified-static-render-indirect-window-join")
        self.assertEqual(summary["original_semantics"], "not-claimed")
        self.assertEqual(rows[0]["raw_words"], "0x00002000")

    def test_render_indirect_indexed_table_scan_is_bounded_and_unresolved(self) -> None:
        rows = [
            {
                "addressing_mode": "index",
                "memory_base_register": "",
                "memory_displacement": "0x2000",
                "source_target_virtual_address": "0x3000",
                "instruction_virtual_address": "0x3010",
            }
        ]
        bases = _indexed_table_bases(rows)
        self.assertEqual(sorted(bases), [0x2000])
        result = {
            "fields": {"base_address": 0},
            "sections": [
                {
                    "name": ".text",
                    "executable": True,
                    "virtual_address": 0x1000,
                    "virtual_size": 0x10,
                    "file_address": 0,
                    "file_size": 0x10,
                },
                {
                    "name": ".data",
                    "executable": False,
                    "virtual_address": 0x2000,
                    "virtual_size": 0x10,
                    "file_address": 0x10,
                    "file_size": 0x10,
                },
            ],
        }
        data = bytes.fromhex(
            "00100000 04100000 00200000 04200000"
            "00100000 04100000 00200000 04200000"
        )
        scanned = scan_table(data, result, 0x2000, bases[0x2000], 8)
        self.assertEqual(scanned["scanned_entries"], "4")
        self.assertEqual(scanned["contiguous_executable_prefix"], "2")
        self.assertEqual(scanned["scan_capped"], "False")
        self.assertEqual(scanned["terminated_after_non_executable_words"], "True")
        self.assertEqual(scanned["candidate_entry_addresses"], "0x00001000|0x00001004")
        self.assertEqual(scanned["original_semantics"], "not-claimed")

    def test_render_indirect_indexed_target_collection_preserves_duplicate_entries(self) -> None:
        collected = collect_indexed_targets([
            {
                "table_base_virtual_address": "0x2000",
                "candidate_entry_addresses": "0x1000|0x1004|0x1000",
            }
        ])
        self.assertEqual(collected["table_rows"], 1)
        targets = collected["targets"]
        self.assertEqual(sorted(targets), [0x1000, 0x1004])
        self.assertEqual(targets[0x1000]["table_entry_indices"], {0, 2})
        self.assertEqual(targets[0x1000]["entry_occurrence_count"], 2)
        self.assertEqual(targets[0x1000]["table_base_addresses"], {"0x00002000"})

    def test_render_indirect_indexed_target_join_is_a_negative_control(self) -> None:
        rows, summary = build_indexed_target_join(
            [{"candidate_target_virtual_address": "0x00001000"}],
            [{"call_target_virtual_address": "0x00001100"}],
            [{"target_virtual_address": "0x00001200"}],
        )
        self.assertEqual(summary["candidate_targets"], 1)
        self.assertEqual(summary["candidate_targets_in_direct_call_targets"], 0)
        self.assertEqual(summary["candidate_targets_in_render_window_targets"], 0)
        self.assertEqual(rows[0]["status"], "verified-structural-negative")
        self.assertEqual(rows[0]["original_semantics"], "not-claimed")

    def test_render_indirect_indexed_cfg_is_bounded_and_does_not_follow_calls(self) -> None:
        data = bytes.fromhex("55 8b ec 75 01 90 c3")
        result = {
            "fields": {"base_address": 0},
            "sections": [{
                "name": ".text",
                "executable": True,
                "virtual_address": 0x1000,
                "virtual_size": len(data),
                "file_address": 0,
                "file_size": len(data),
            }],
        }
        row = analyze_indexed_target(
            data,
            result,
            {
                "candidate_target_virtual_address": "0x1000",
                "table_entry_indices": "0|2",
                "entry_occurrence_count": "2",
            },
            len(data),
        )
        self.assertEqual(row["reachable_instruction_count"], "5")
        self.assertEqual(row["reachable_ret_count"], "1")
        self.assertEqual(row["conditional_branch_count"], "1")
        self.assertEqual(row["internal_branch_target_count"], "1")
        self.assertEqual(row["direct_call_count"], "0")
        self.assertEqual(row["indirect_control_transfer_count"], "0")
        self.assertEqual(row["status"], "verified-bounded-static-render-indirect-indexed-cfg-slices")
        self.assertEqual(row["original_semantics"], "not-claimed")

    def test_render_indirect_indexed_cfg_join_is_a_negative_control(self) -> None:
        rows, summary = build_indexed_cfg_join(
            [
                {"direct_call_target_addresses": "0x00001000"},
                {"direct_call_target_addresses": "0x00001000"},
            ],
            [{"call_target_virtual_address": "0x00002000"}],
            [{"target_virtual_address": "0x00003000"}],
            [{"call_target_virtual_address": "0x00004000"}],
        )
        self.assertEqual(summary["cfg_direct_call_targets"], 1)
        self.assertEqual(summary["cfg_targets_in_render_direct_call_targets"], 0)
        self.assertEqual(summary["cfg_targets_in_render_window_targets"], 0)
        self.assertEqual(summary["cfg_targets_in_render_call_graph_nodes"], 0)
        self.assertEqual(summary["status"], "verified-structural-negative")
        self.assertEqual(rows[0]["cfg_direct_call_occurrence_count"], "2")
        self.assertEqual(rows[0]["original_semantics"], "not-claimed")

    def test_render_indirect_indexed_cfg_call_targets_preserve_distinct_addresses(self) -> None:
        targets = collect_call_targets(
            [
                {
                    "candidate_target_virtual_address": "0x00001000",
                    "direct_call_target_addresses": "0x0016BF90",
                },
                {
                    "candidate_target_virtual_address": "0x00001010",
                    "direct_call_target_addresses": "0x0016BF90",
                },
            ]
        )
        self.assertEqual(sorted(targets), [0x0016BF90])
        self.assertEqual(targets[0x0016BF90]["source_row_count"], 2)
        self.assertEqual(
            targets[0x0016BF90]["source_candidates"],
            {"0x00001000", "0x00001010"},
        )

    def test_render_indirect_indexed_cfg_call_site_summary_is_local_only(self) -> None:
        summary = build_indexed_cfg_call_site_summary(
            "fixture.iso",
            "default.xbe",
            "XBE",
            10,
            [{"candidate_target_virtual_address": "0x00001000"}],
            [
                {
                    "source_candidate_target_virtual_address": "0x00001000",
                    "call_target_virtual_address": "0x0016BF90",
                    "has_push_edx_before_lea": "True",
                    "has_lea_ecx_base_before_call": "True",
                    "lea_displacement": "0x00000224",
                    "has_ret_imm_4_after_call": "True",
                }
            ],
            256,
        )
        self.assertEqual(summary["direct_call_occurrences"], "1")
        self.assertEqual(summary["distinct_call_targets"], "1")
        self.assertEqual(summary["distinct_lea_displacements"], "1")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_render_indirect_indexed_cfg_call_target_cfg_summary_is_bounded(self) -> None:
        summary = build_indexed_cfg_call_target_cfg_summary(
            "fixture.iso",
            "default.xbe",
            "XBE",
            10,
            [{
                "call_target_virtual_address": "0x0016BF90",
                "source_cfg_row_count": "2",
                "source_candidate_targets": "0x00001000|0x00001010",
            }],
            [{
                "reachable_instruction_count": "4",
                "reachable_ret_count": "1",
                "linear_instruction_count": "6",
                "basic_block_count": "2",
                "conditional_branch_count": "1",
                "unconditional_branch_count": "0",
                "internal_branch_target_count": "1",
                "out_of_window_branch_target_count": "0",
                "direct_call_count": "1",
                "unique_direct_call_target_count": "1",
                "direct_call_target_addresses": "0x0026E278",
                "indirect_control_transfer_count": "0",
            }],
            256,
        )
        self.assertEqual(summary["targets_with_reachable_cfg"], "1")
        self.assertEqual(summary["total_direct_calls"], "1")
        self.assertEqual(summary["distinct_direct_call_targets"], "1")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_render_indirect_indexed_cfg_call_site_join_preserves_duplicates(self) -> None:
        joined = join_indexed_cfg_call_site_rows(
            [{
                "candidate_target_virtual_address": "0x00001000",
                "table_base_addresses": "0x00002000",
                "table_entry_indices": "4|9",
                "entry_occurrence_count": "2",
            }],
            [{
                "source_candidate_target_virtual_address": "0x00001000",
                "call_instruction_virtual_address": "0x00001010",
                "call_target_virtual_address": "0x00003000",
                "has_push_edx_before_lea": "True",
                "has_lea_ecx_base_before_call": "True",
                "has_ret_imm_4_after_call": "True",
            }],
        )
        summary = build_indexed_cfg_call_site_join_summary(
            "fixture.iso",
            "default.xbe",
            "XBE",
            10,
            [{
                "table_base_addresses": "0x00002000",
                "table_entry_indices": "4|9",
            }],
            [{"call_target_virtual_address": "0x00003000"}],
            joined,
        )
        self.assertEqual(len(joined), 1)
        self.assertEqual(summary["distinct_table_entry_indices"], "2")
        self.assertEqual(summary["duplicate_table_occurrences_over_call_sites"], "1")
        self.assertEqual(summary["call_sites_with_complete_local_shape"], "1")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_render_indirect_indexed_cfg_nested_call_targets_are_one_hop(self) -> None:
        targets = collect_nested_targets([
            {
                "call_target_virtual_address": "0x0016BF90",
                "direct_call_count": "2",
                "direct_call_target_addresses": "0x0026E278|0x0026E278",
            }
        ])
        self.assertEqual(sorted(targets), [0x0026E278])
        self.assertEqual(targets[0x0026E278]["source_parents"], {"0x0016BF90"})
        self.assertEqual(targets[0x0026E278]["call_occurrences"], 2)
        summary = build_indexed_cfg_nested_call_target_cfg_summary(
            "fixture.iso",
            "default.xbe",
            "XBE",
            10,
            [{"direct_call_count": "2"}],
            [{
                "reachable_instruction_count": "37",
                "reachable_ret_count": "1",
                "linear_instruction_count": "89",
                "basic_block_count": "7",
                "conditional_branch_count": "3",
                "unconditional_branch_count": "2",
                "internal_branch_target_count": "4",
                "out_of_window_branch_target_count": "0",
                "direct_call_count": "0",
                "unique_direct_call_target_count": "0",
                "direct_call_target_addresses": "",
                "indirect_control_transfer_count": "0",
            }],
            256,
        )
        self.assertEqual(summary["parent_direct_call_occurrences"], "2")
        self.assertEqual(summary["targets_with_reachable_cfg"], "1")
        self.assertEqual(summary["total_direct_calls"], "0")
        self.assertEqual(summary["original_semantics"], "not-claimed")

    def test_render_indirect_indexed_cfg_nested_call_sites_preserve_local_context(self) -> None:
        joined = filter_nested_call_sites(
            [{
                "call_target_virtual_address": "0x0016BF90",
                "direct_call_target_addresses": "0x0026E278",
            }],
            [{
                "source_candidate_target_virtual_address": "0x0016BF90",
                "call_instruction_virtual_address": "0x0016BFAD",
                "call_target_virtual_address": "0x0026E278",
                "context_instructions": "call 0x26e278|ret 8",
                "has_push_edx_before_lea": "False",
                "has_lea_ecx_base_before_call": "False",
                "lea_displacement": "",
                "has_ret_imm_4_after_call": "False",
            }],
        )
        summary = build_indexed_cfg_nested_call_site_summary(
            "fixture.iso",
            "default.xbe",
            "XBE",
            10,
            [{"call_target_virtual_address": "0x0016BF90"}],
            joined,
            256,
        )
        self.assertEqual(len(joined), 1)
        self.assertEqual(summary["nested_call_target_rows"], "1")
        self.assertEqual(summary["distinct_nested_call_targets"], "1")
        self.assertEqual(summary["nested_call_sites_with_ret_imm_4_after_call"], "0")
        self.assertEqual(summary["original_semantics"], "not-claimed")


if __name__ == "__main__":
    unittest.main()
