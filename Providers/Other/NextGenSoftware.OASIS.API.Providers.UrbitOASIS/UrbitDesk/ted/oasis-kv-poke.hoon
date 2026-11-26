::  Applies one action through a real poke and completes only on the agent's
::  poke-ack. A nack fails the thread, so spider answers the HTTP request 500.
::
/-  spider, oasis-kv
/+  strandio
=,  strand=strand:spider
^-  thread:spider
|=  arg=vase
=/  m  (strand ,vase)
^-  form:m
=/  act  !<([~ action:oasis-kv] arg)
;<  =bowl:spider  bind:m  get-bowl:strandio
;<  ~  bind:m  (poke:strandio [our.bowl %oasis-kv] oasis-kv-action+!>(+.act))
(pure:m !>(`json`s+'ack'))
