package brain

import (
	"fmt"
	"regexp"
	"strconv"

	"go.mongodb.org/mongo-driver/bson"
)

var operationMapping = map[string]string{
	"<":  "$lt",
	">":  "$gt",
	">=": "$gte",
	"<=": "$lte",
	"=":  "$eq",
	"!=": "$neq",
}

type Operator func(a float64, b float64) bool
type Filter func(a float64) bool

var operationFunctionMapping = map[string]Operator{
	"<":  func(a float64, b float64) bool { return a < b },
	">":  func(a float64, b float64) bool { return a > b },
	">=": func(a float64, b float64) bool { return a >= b },
	"<=": func(a float64, b float64) bool { return a <= b },
	"=":  func(a float64, b float64) bool { return a == b },
	"!=": func(a float64, b float64) bool { return a != b },
}

var AvailableNeuronFields = map[string]bool{
	"fired":               true,
	"fired_fraction":      true,
	"activity":            true,
	"dampening":           true,
	"current_calcium":     true,
	"target_calcium":      true,
	"synaptic_input":      true,
	"background_input":    true,
	"grown_axons":         true,
	"connected_axons":     true,
	"grown_dendrites":     true,
	"connected_dendrites": true,
	"community_level1":    true,
	"community_level2":    true,
	"community_level3":    true,
	"community_level4":    true,
	"timestep":            true,
	"step":                true,
	// "neuron_id":           true,
}

var AvailableClusterFields = map[string]bool{
	"max_fired":               true,
	"max_fired_fraction":      true,
	"max_activity":            true,
	"max_dampening":           true,
	"max_current_calcium":     true,
	"max_target_calcium":      true,
	"max_synaptic_input":      true,
	"max_background_input":    true,
	"max_grown_axons":         true,
	"max_connected_axons":     true,
	"max_grown_dendrites":     true,
	"max_connected_dendrites": true,
	"min_fired":               true,
	"min_fired_fraction":      true,
	"min_activity":            true,
	"min_dampening":           true,
	"min_current_calcium":     true,
	"min_target_calcium":      true,
	"min_synaptic_input":      true,
	"min_background_input":    true,
	"min_grown_axons":         true,
	"min_connected_axons":     true,
	"min_grown_dendrites":     true,
	"min_connected_dendrites": true,
	"avg_fired":               true,
	"avg_fired_fraction":      true,
	"avg_activity":            true,
	"avg_dampening":           true,
	"avg_current_calcium":     true,
	"avg_target_calcium":      true,
	"avg_synaptic_input":      true,
	"avg_background_input":    true,
	"avg_grown_axons":         true,
	"avg_connected_axons":     true,
	"avg_grown_dendrites":     true,
	"avg_connected_dendrites": true,
	"timestep":                true,
	"step":                    true,
}

func ParseFilters(filters []string) (bson.M, error) {
	filtersMap := make(map[string]map[string]float64)

	for _, filter := range filters {
		regex_pattern := "(?P<field>[a-z_]+) *(?P<operator><=|<|>=|>|!=|=) *(?P<value>\\S+)"
		reg := regexp.MustCompile(regex_pattern)
		matches := reg.FindStringSubmatch(filter)

		if len(matches) < 3 {
			return nil, fmt.Errorf("invalid filter: %s", filter)
		}

		rawOperator := matches[reg.SubexpIndex("operator")]
		rawField := matches[reg.SubexpIndex("field")]
		rawValue := matches[reg.SubexpIndex("value")]
		var operator string
		var ok bool

		if operator, ok = operationMapping[rawOperator]; !ok {
			return nil, fmt.Errorf("invalid operator: %s", rawOperator)
		}

		value, err := strconv.ParseFloat(rawValue, 64)
		if err != nil {
			return nil, fmt.Errorf("invalid filter value: %s", rawValue)
		}

		if _, ok := AvailableNeuronFields[rawField]; !ok {
			return nil, fmt.Errorf("invalid field value: %s", rawField)
		}

		filterKey := rawField

		// because mango's field is called step
		if filterKey == "timestep" {
			filterKey = "step"

			if value == 1000000 {
				value = 999900
			}
		}

		if fieldMap, ok := filtersMap[filterKey]; ok {
			fieldMap[operator] = value
		} else {
			filtersMap[filterKey] = map[string]float64{operator: value}
		}

	}

	bsonMap := bson.M{}
	for field, filter := range filtersMap {
		fieldBson := bson.M{}
		for operator, value := range filter {
			fieldBson[operator] = value
		}
		bsonMap[field] = fieldBson
	}

	return bsonMap, nil
}

func getTimestepAndFilterFromFilters(filters []string) (int, string, Filter, error) {
	timestep := -1
	filterField := ""
	var resultFilter Filter
	for _, filter := range filters {
		regex_pattern := "(?P<field>[a-z_]+) *(?P<operator><=|<|>=|>|!=|=) *(?P<value>\\S+)"
		reg := regexp.MustCompile(regex_pattern)
		matches := reg.FindStringSubmatch(filter)

		// if len(matches) < 3 {
		// 	return nil, fmt.Errorf("invalid filter: %s", filter)
		// }

		rawOperator := matches[reg.SubexpIndex("operator")]
		rawField := matches[reg.SubexpIndex("field")]
		rawValue := matches[reg.SubexpIndex("value")]
		var operator Operator
		var ok bool

		if operator, ok = operationFunctionMapping[rawOperator]; !ok {
			return 0, "", nil, fmt.Errorf("invalid operator: %s", rawOperator)
		}

		value, err := strconv.ParseFloat(rawValue, 64)
		if err != nil {
			return 0, "", nil, fmt.Errorf("invalid filter value: %s", rawValue)
		}

		if _, ok := AvailableNeuronFields[rawField]; !ok {
			return 0, "", nil, fmt.Errorf("invalid field value: %s", rawField)
		}
		if rawField == "timestep" || rawField == "step" {
			if value == 1_000_000 {
				timestep = 999_900
			} else {
				timestep = int(value)
			}
		} else {
			if filterField == "" {
				filterField = rawField
				resultFilter = func(a float64) bool { return operator(a, value) }
			}
		}
	}
	if timestep == -1 {
		return 0, "", nil, fmt.Errorf("please specify timestep in filters")
	}
	return timestep, filterField, resultFilter, nil
}

func ParseSingleFilter(filter string) (Filter, error) {
	regex_pattern := ".*(?P<operator><=|<|>=|>|!=|=) *(?P<value>\\S+)"
	reg := regexp.MustCompile(regex_pattern)
	matches := reg.FindStringSubmatch(filter)

	if len(matches) < 2 {
		return nil, fmt.Errorf("invalid filter: %s", filter)
	}

	rawOperator := matches[reg.SubexpIndex("operator")]
	rawValue := matches[reg.SubexpIndex("value")]
	var operator Operator
	var ok bool

	if operator, ok = operationFunctionMapping[rawOperator]; !ok {
		return nil, fmt.Errorf("invalid operator: %s", rawOperator)
	}

	value, err := strconv.ParseFloat(rawValue, 64)
	if err != nil {
		return nil, fmt.Errorf("invalid filter value: %s", rawValue)
	}

	return func(a float64) bool { return operator(a, value) }, nil

}

func ParseNeuronProjection(projection []string, everything bool) (bson.D, error) {
	// should return something like this: bson.D{{"_id", 0}, {"neuron_id", 1}, {"current_calcium", 1}, {"fired_fraction", 1}}

	bsonProjection := bson.D{{Key: "_id", Value: 0}, {Key: "neuron_id", Value: 1}}

	// add all the fields to the projection
	if everything {
		for field := range AvailableNeuronFields {
			// because mango's field is called step
			if field == "timestep" {
				field = "step"
			}
			if field == "neuron_id" {
				continue
			}
			bsonProjection = append(bsonProjection, bson.E{Key: field, Value: 1})
		}
		return bsonProjection, nil
	}

	// otherrwise add only the field requested in the query
	for _, field := range projection {
		if member, ok := AvailableNeuronFields[field]; ok && member {
			// because mango's field is called step
			if field == "timestep" {
				field = "step"
			}
			bsonProjection = append(bsonProjection, bson.E{Key: field, Value: 1})
		} else {
			return nil, fmt.Errorf("invalid projection field: %s", field)
		}
	}

	return bsonProjection, nil
}

func ParseClusterProjection(projection []string) (bson.D, error) {
	// should return something like this: bson.D{{"_id", 0}, {"neuron_id", 1}, {"current_calcium", 1}, {"fired_fraction", 1}}

	bsonProjection := bson.D{{Key: "_id", Value: 0}, {Key: "id", Value: 1}, {Key: "community_id", Value: 1}, {Key: "leaves_count", Value: 1}, {Key: "children_count", Value: 1}}

	// add only the field requested in the query
	for _, field := range projection {
		if member, ok := AvailableClusterFields[field]; ok && member {
			// because mango's field is called step
			if field == "timestep" {
				field = "step"
			}
			bsonProjection = append(bsonProjection, bson.E{Key: field, Value: 1})
		} else {
			return nil, fmt.Errorf("invalid projection field: %s", field)
		}
	}

	return bsonProjection, nil
}
