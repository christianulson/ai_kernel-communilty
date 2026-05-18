from __future__ import annotations

import json
import os
from typing import Any, Dict, Optional


class MySQLStore:
    def __init__(
        self,
        host: str = "",
        port: int = 3306,
        database: str = "krnlai",
        user: str = "",
        password: str = "",
    ) -> None:
        self._host = host or os.getenv("MYSQL_HOST", "localhost")
        self._port = int(os.getenv("MYSQL_PORT", str(port)))
        self._database = database or os.getenv("MYSQL_DATABASE", "krnlai")
        self._user = user or os.getenv("MYSQL_USER", "root")
        self._password = password or os.getenv("MYSQL_PASSWORD", "")
        self._pool = None

    def _ensure_pool(self) -> None:
        if self._pool is None:
            try:
                import importlib.util
                if importlib.util.find_spec("aiomysql") is None:
                    raise ImportError
                self._pool = None
            except ImportError:
                raise ImportError("aiomysql package required: pip install aiomysql")

    async def _get_conn(self):
        import aiomysql
        return await aiomysql.connect(
            host=self._host,
            port=self._port,
            db=self._database,
            user=self._user,
            password=self._password,
            autocommit=True,
        )

    async def execute(self, query: str, params: Optional[tuple] = None) -> Any:
        conn = await self._get_conn()
        try:
            async with conn.cursor() as cursor:
                await cursor.execute(query, params or ())
                if query.strip().upper().startswith("SELECT"):
                    return await cursor.fetchall()
                return cursor.rowcount
        finally:
            conn.close()

    async def store_cycle(self, cycle_id: str, data: Dict[str, Any]) -> None:
        await self.execute(
            "INSERT INTO cycles (id, data, created_at) VALUES (%s, %s, NOW()) "
            "ON DUPLICATE KEY UPDATE data = %s",
            (cycle_id, json.dumps(data), json.dumps(data)),
        )

    async def get_cycle(self, cycle_id: str) -> Optional[Dict[str, Any]]:
        rows = await self.execute(
            "SELECT data FROM cycles WHERE id = %s",
            (cycle_id,),
        )
        if rows:
            return json.loads(rows[0][0])
        return None

    async def health(self) -> Dict[str, Any]:
        try:
            await self.execute("SELECT 1")
            return {"status": "ok", "database": self._database}
        except Exception as e:
            return {"status": "error", "message": str(e)}
