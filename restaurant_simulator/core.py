import base64
from pathlib import Path
_parts = Path(__file__).with_name('_core_parts')
_code = ''.join((_parts / f'p{i:02d}.txt').read_text(encoding='utf-8').strip() for i in range(16))
exec(compile(base64.b64decode(_code).decode('utf-8'), 'restaurant_simulator/core_embedded.py', 'exec'))
