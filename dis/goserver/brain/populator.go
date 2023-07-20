package brain

import (
	"go.mongodb.org/mongo-driver/bson"
)

func PopulateGenericNeuron(propertyName string, propertyValue float64, neuron GenericNeuron) GenericNeuron {

	switch propertyName {
	case "fired":
		neuron.Fired = propertyValue > 0.5
	case "fired_fraction":
		neuron.FiredFraction = float32(propertyValue)
	case "activity":
		neuron.Activity = float32(propertyValue)
	case "dampening":
		neuron.Dampening = float32(propertyValue)
	case "current_calcium":
		neuron.CurrentCalcium = float32(propertyValue)
	case "target_calcium":
		neuron.TargetCalcium = float32(propertyValue)
	case "synaptic_input":
		neuron.SynapticInput = float32(propertyValue)
	case "background_input":
		neuron.BackgroundInput = float32(propertyValue)
	case "grown_axons":
		neuron.GrownAxons = float32(propertyValue)
	case "connected_axons":
		neuron.ConnectedAxons = int32(propertyValue)
	case "grown_dendrites":
		neuron.GrownDendrites = float32(propertyValue)
	case "connected_dendrites":
		neuron.ConnectedDendrites = int32(propertyValue)
	case "community_level1":
		neuron.CommunityLevel1 = int32(propertyValue)
	case "community_level2":
		neuron.CommunityLevel2 = int32(propertyValue)
	case "community_level3":
		neuron.CommunityLevel3 = int32(propertyValue)
	case "community_level4":
		neuron.CommunityLevel4 = int32(propertyValue)
	case "step":
		neuron.Timestep = uint32(propertyValue)
	}

	return neuron
}

func GetPopulatedGenericNeuron(result map[string]float64, mongoProjection bson.D) *GenericNeuron {

	genericNeuron := GenericNeuron{}
	for _, entry := range mongoProjection {
		if entry.Value.(int) < 1 {
			continue
		}
		field := entry.Key
		switch field {
		case "fired":
			genericNeuron.Fired = result[field] > 0.5
		case "fired_fraction":
			genericNeuron.FiredFraction = float32(result[field])
		case "activity":
			genericNeuron.Activity = float32(result[field])
		case "dampening":
			genericNeuron.Dampening = float32(result[field])
		case "current_calcium":
			genericNeuron.CurrentCalcium = float32(result[field])
		case "target_calcium":
			genericNeuron.TargetCalcium = float32(result[field])
		case "synaptic_input":
			genericNeuron.SynapticInput = float32(result[field])
		case "background_input":
			genericNeuron.BackgroundInput = float32(result[field])
		case "grown_axons":
			genericNeuron.GrownAxons = float32(result[field])
		case "connected_axons":
			genericNeuron.ConnectedAxons = int32(result[field])
		case "grown_dendrites":
			genericNeuron.GrownDendrites = float32(result[field])
		case "connected_dendrites":
			genericNeuron.ConnectedDendrites = int32(result[field])
		case "community_level1":
			genericNeuron.CommunityLevel1 = int32(result[field])
		case "community_level2":
			genericNeuron.CommunityLevel2 = int32(result[field])
		case "community_level3":
			genericNeuron.CommunityLevel3 = int32(result[field])
		case "community_level4":
			genericNeuron.CommunityLevel4 = int32(result[field])
		case "step":
			genericNeuron.Timestep = uint32(result[field])
		}

	}

	return &genericNeuron
}

func GetPopulatedCluster(result map[string]float64, mongoProjection bson.D) *Cluster {
	cluster := Cluster{}
	for _, entry := range mongoProjection {
		if entry.Value.(int) < 1 {
			continue
		}
		field := entry.Key
		switch field {
		case "community_level1":
			// if we get neurons in a level 1 cluster, the name of parent cluster is different:
			cluster.ParentClusterId = uint32(result[field])
		case "community_id":
			cluster.ParentClusterId = uint32(result[field])
		case "id":
			cluster.ClusterId = uint32(result[field])
		case "neuron_id":
			// if we get neurons in a level 1 cluster, the name of the node id is same as neuron id:
			cluster.ClusterId = uint32(result[field])
		case "leaves_count":
			cluster.LeavesCount = uint32(result[field])
		case "children_count":
			cluster.ChildrenCount = uint32(result[field])
		case "max_fired":
			cluster.MaxFired = float32(result[field])
		case "max_fired_fraction":
			cluster.MaxFiredFraction = float32(result[field])
		case "max_activity":
			cluster.MaxActivity = float32(result[field])
		case "max_dampening":
			cluster.MaxDampening = float32(result[field])
		case "max_current_calcium":
			cluster.MaxCurrentCalcium = float32(result[field])
		case "max_target_calcium":
			cluster.MaxTargetCalcium = float32(result[field])
		case "max_synaptic_input":
			cluster.MaxSynapticInput = float32(result[field])
		case "max_background_input":
			cluster.MaxBackgroundInput = float32(result[field])
		case "max_grown_axons":
			cluster.MaxGrownAxons = float32(result[field])
		case "max_connected_axons":
			cluster.MaxConnectedAxons = float32(result[field])
		case "max_grown_dendrites":
			cluster.MaxGrownDendrites = float32(result[field])
		case "max_connected_dendrites":
			cluster.MaxConnectedDendrites = float32(result[field])
		case "min_fired":
			cluster.MinFired = float32(result[field])
		case "min_fired_fraction":
			cluster.MinFiredFraction = float32(result[field])
		case "min_activity":
			cluster.MinActivity = float32(result[field])
		case "min_dampening":
			cluster.MinDampening = float32(result[field])
		case "min_current_calcium":
			cluster.MinCurrentCalcium = float32(result[field])
		case "min_target_calcium":
			cluster.MinTargetCalcium = float32(result[field])
		case "min_synaptic_input":
			cluster.MinSynapticInput = float32(result[field])
		case "min_background_input":
			cluster.MinBackgroundInput = float32(result[field])
		case "min_grown_axons":
			cluster.MinGrownAxons = float32(result[field])
		case "min_connected_axons":
			cluster.MinConnectedAxons = float32(result[field])
		case "min_grown_dendrites":
			cluster.MinGrownDendrites = float32(result[field])
		case "min_connected_dendrites":
			cluster.MinConnectedDendrites = float32(result[field])
		case "avg_fired":
			cluster.AvgFired = float32(result[field])
		case "avg_fired_fraction":
			cluster.AvgFiredFraction = float32(result[field])
		case "avg_activity":
			cluster.AvgActivity = float32(result[field])
		case "avg_dampening":
			cluster.AvgDampening = float32(result[field])
		case "avg_current_calcium":
			cluster.AvgCurrentCalcium = float32(result[field])
		case "avg_target_calcium":
			cluster.AvgTargetCalcium = float32(result[field])
		case "avg_synaptic_input":
			cluster.AvgSynapticInput = float32(result[field])
		case "avg_background_input":
			cluster.AvgBackgroundInput = float32(result[field])
		case "avg_grown_axons":
			cluster.AvgGrownAxons = float32(result[field])
		case "avg_connected_axons":
			cluster.AvgConnectedAxons = float32(result[field])
		case "avg_grown_dendrites":
			cluster.AvgGrownDendrites = float32(result[field])
		case "avg_connected_dendrites":
			cluster.AvgConnectedDendrites = float32(result[field])
		case "step":
			cluster.Timestep = uint32(result[field])
		}

	}

	return &cluster
}

func GetPopulatedClusterSingleValue(value float64, attribute string, cluster Cluster) Cluster {

	switch attribute {
	case "leaves_count":
		cluster.LeavesCount = uint32(value)
	case "children_count":
		cluster.ChildrenCount = uint32(value)
	case "max_fired":
		cluster.MaxFired = float32(value)
	case "max_fired_fraction":
		cluster.MaxFiredFraction = float32(value)
	case "max_activity":
		cluster.MaxActivity = float32(value)
	case "max_dampening":
		cluster.MaxDampening = float32(value)
	case "max_current_calcium":
		cluster.MaxCurrentCalcium = float32(value)
	case "max_target_calcium":
		cluster.MaxTargetCalcium = float32(value)
	case "max_synaptic_input":
		cluster.MaxSynapticInput = float32(value)
	case "max_background_input":
		cluster.MaxBackgroundInput = float32(value)
	case "max_grown_axons":
		cluster.MaxGrownAxons = float32(value)
	case "max_connected_axons":
		cluster.MaxConnectedAxons = float32(value)
	case "max_grown_dendrites":
		cluster.MaxGrownDendrites = float32(value)
	case "max_connected_dendrites":
		cluster.MaxConnectedDendrites = float32(value)
	case "min_fired":
		cluster.MinFired = float32(value)
	case "min_fired_fraction":
		cluster.MinFiredFraction = float32(value)
	case "min_activity":
		cluster.MinActivity = float32(value)
	case "min_dampening":
		cluster.MinDampening = float32(value)
	case "min_current_calcium":
		cluster.MinCurrentCalcium = float32(value)
	case "min_target_calcium":
		cluster.MinTargetCalcium = float32(value)
	case "min_synaptic_input":
		cluster.MinSynapticInput = float32(value)
	case "min_background_input":
		cluster.MinBackgroundInput = float32(value)
	case "min_grown_axons":
		cluster.MinGrownAxons = float32(value)
	case "min_connected_axons":
		cluster.MinConnectedAxons = float32(value)
	case "min_grown_dendrites":
		cluster.MinGrownDendrites = float32(value)
	case "min_connected_dendrites":
		cluster.MinConnectedDendrites = float32(value)
	case "avg_fired":
		cluster.AvgFired = float32(value)
	case "avg_fired_fraction":
		cluster.AvgFiredFraction = float32(value)
	case "avg_activity":
		cluster.AvgActivity = float32(value)
	case "avg_dampening":
		cluster.AvgDampening = float32(value)
	case "avg_current_calcium":
		cluster.AvgCurrentCalcium = float32(value)
	case "avg_target_calcium":
		cluster.AvgTargetCalcium = float32(value)
	case "avg_synaptic_input":
		cluster.AvgSynapticInput = float32(value)
	case "avg_background_input":
		cluster.AvgBackgroundInput = float32(value)
	case "avg_grown_axons":
		cluster.AvgGrownAxons = float32(value)
	case "avg_connected_axons":
		cluster.AvgConnectedAxons = float32(value)
	case "avg_grown_dendrites":
		cluster.AvgGrownDendrites = float32(value)
	case "avg_connected_dendrites":
		cluster.AvgConnectedDendrites = float32(value)
	case "step":
		cluster.Timestep = uint32(value)
	}

	return cluster
}
