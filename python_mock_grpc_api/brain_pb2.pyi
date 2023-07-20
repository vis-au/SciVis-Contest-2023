from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Iterable as _Iterable, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class AllNeuronsQuery(_message.Message):
    __slots__ = ["simulation_id", "timestep"]
    SIMULATION_ID_FIELD_NUMBER: _ClassVar[int]
    TIMESTEP_FIELD_NUMBER: _ClassVar[int]
    simulation_id: int
    timestep: int
    def __init__(self, timestep: _Optional[int] = ..., simulation_id: _Optional[int] = ...) -> None: ...

class AllSynapsesQuery(_message.Message):
    __slots__ = ["simulation_id", "timestep"]
    SIMULATION_ID_FIELD_NUMBER: _ClassVar[int]
    TIMESTEP_FIELD_NUMBER: _ClassVar[int]
    simulation_id: int
    timestep: int
    def __init__(self, timestep: _Optional[int] = ..., simulation_id: _Optional[int] = ...) -> None: ...

class Neuron(_message.Message):
    __slots__ = ["calcium", "fired_fraction", "id"]
    CALCIUM_FIELD_NUMBER: _ClassVar[int]
    FIRED_FRACTION_FIELD_NUMBER: _ClassVar[int]
    ID_FIELD_NUMBER: _ClassVar[int]
    calcium: int
    fired_fraction: float
    id: int
    def __init__(self, id: _Optional[int] = ..., calcium: _Optional[int] = ..., fired_fraction: _Optional[float] = ...) -> None: ...

class NeuronReply(_message.Message):
    __slots__ = ["neurons"]
    NEURONS_FIELD_NUMBER: _ClassVar[int]
    neurons: _containers.RepeatedCompositeFieldContainer[Neuron]
    def __init__(self, neurons: _Optional[_Iterable[_Union[Neuron, _Mapping]]] = ...) -> None: ...

class Synapse(_message.Message):
    __slots__ = ["from_id", "to_id", "weight"]
    FROM_ID_FIELD_NUMBER: _ClassVar[int]
    TO_ID_FIELD_NUMBER: _ClassVar[int]
    WEIGHT_FIELD_NUMBER: _ClassVar[int]
    from_id: int
    to_id: int
    weight: int
    def __init__(self, from_id: _Optional[int] = ..., to_id: _Optional[int] = ..., weight: _Optional[int] = ...) -> None: ...

class SynapseReply(_message.Message):
    __slots__ = ["synapses"]
    SYNAPSES_FIELD_NUMBER: _ClassVar[int]
    synapses: _containers.RepeatedCompositeFieldContainer[Synapse]
    def __init__(self, synapses: _Optional[_Iterable[_Union[Synapse, _Mapping]]] = ...) -> None: ...
