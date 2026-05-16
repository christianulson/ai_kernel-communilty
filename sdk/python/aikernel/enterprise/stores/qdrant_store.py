from __future__ import annotations

import os
from typing import Any, Dict, List, Optional


class QdrantStore:
    def __init__(
        self,
        host: str = "",
        port: int = 6333,
        collection: str = "aikernel",
        api_key: Optional[str] = None,
    ) -> None:
        self._host = host or os.getenv("QDRANT_HOST", "localhost")
        self._port = int(os.getenv("QDRANT_PORT", str(port)))
        self._collection = collection
        self._api_key = api_key or os.getenv("QDRANT_API_KEY", "")
        self._client = None

    def _ensure_client(self) -> None:
        if self._client is None:
            try:
                from qdrant_client import QdrantClient
                self._client = QdrantClient(
                    host=self._host,
                    port=self._port,
                    api_key=self._api_key or None,
                )
                self._ensure_collection()
            except ImportError:
                raise ImportError("qdrant-client package required: pip install qdrant-client")

    def _ensure_collection(self) -> None:
        collections = self._client.get_collections().collections
        exists = any(c.name == self._collection for c in collections)
        if not exists:
            self._client.create_collection(
                collection_name=self._collection,
                vectors_config={"size": 1536, "distance": "Cosine"},
            )

    async def upsert(self, point_id: str, vector: List[float], payload: Dict[str, Any]) -> None:
        self._ensure_client()
        self._client.upsert(
            collection_name=self._collection,
            points=[{"id": point_id, "vector": vector, "payload": payload}],
        )

    async def search(self, vector: List[float], limit: int = 10) -> List[Dict[str, Any]]:
        self._ensure_client()
        results = self._client.search(
            collection_name=self._collection,
            query_vector=vector,
            limit=limit,
        )
        return [
            {"id": str(r.id), "score": r.score, "payload": r.payload}
            for r in results
        ]

    async def delete(self, point_id: str) -> None:
        self._ensure_client()
        self._client.delete(
            collection_name=self._collection,
            points_selector=[point_id],
        )

    async def health(self) -> Dict[str, Any]:
        self._ensure_client()
        info = self._client.get_collections()
        return {"status": "ok", "collections": len(info.collections)}
