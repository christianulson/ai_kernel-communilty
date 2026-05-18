class StoreError(Exception):
    pass


class ItemNotFoundError(StoreError):
    pass


class DuplicateItemError(StoreError):
    pass


class StoreCapacityError(StoreError):
    pass
