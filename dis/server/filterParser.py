from typing import List
import re


class ParseFilterError(Exception):
    pass


class FilterParser:

    AVAILABLE_OPERATIONS = ["<", ">", "=", "<=", "<="]
    OPERATION_MAPPING = {
        "<": "$lt",
        ">": "$gt",
        ">=": "$gte",
        "<=": "$lte",
        "=": "$eq",
        "!=": "$neq",
    }

    def __init__(self):
        pass

    def parse(self, filter_strings: List[str] = ["current_calcium < 0.004"]):
        """
        returns a mongo-usable filter dict that models the filters in `filters`
        """
        regex_pattern = "([a-z_]+) *(<=|<|>=|>|!=|=) *(\S+)"

        filters = {}
        for filter_string in filter_strings:
            search_result = re.search(regex_pattern, filter_string)

            if search_result:
                field = search_result.group(1)
                operator = search_result.group(2)
                value = search_result.group(3)

                if operator not in self.OPERATION_MAPPING.keys():
                    continue

                try:
                    value = float(value)
                except ValueError:
                    raise ParseFilterError("Cannot convert value to float")

                filter_key = field
                filter_value = {self.OPERATION_MAPPING[operator]: value}

                if filters.get(filter_key) is None:
                    filters[filter_key] = filter_value
                else:
                    filters[filter_key].update(filter_value)

        return filters


if __name__ == "__main__":
    fp = FilterParser()
    print(
        fp.parse(
            [
                "current_calcium < 0.004",
                "current_calcium > 0.003984",
                "fired_fraction= 9",
                "activity <=2",
                "activity>=0",
            ]
        )
    )
