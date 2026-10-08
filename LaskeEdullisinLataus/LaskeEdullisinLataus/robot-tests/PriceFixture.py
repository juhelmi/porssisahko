import json
import tempfile
from datetime import datetime, timedelta, timezone
from pathlib import Path


class PriceFixture:
    def __init__(self):
        self.file_path = None

    def create_price_fixture(self):
        start = datetime.now(timezone.utc).replace(minute=0, second=0, microsecond=0)
        start += timedelta(hours=1)
        prices = [
            {
                "price": price,
                "startDate": (start + timedelta(hours=index)).isoformat(),
                "endDate": (start + timedelta(hours=index + 1)).isoformat(),
            }
            for index, price in enumerate([10, 9, 8, 7, 6, 5, 4, 3, 2, 1])
        ]
        with tempfile.NamedTemporaryFile(mode="w", suffix=".json", delete=False, encoding="utf-8") as fixture:
            json.dump({"status": "success", "prices": prices}, fixture)
            self.file_path = Path(fixture.name)
        return str(self.file_path)

    def delete_price_fixture(self):
        if self.file_path is not None:
            self.file_path.unlink(missing_ok=True)
            self.file_path = None