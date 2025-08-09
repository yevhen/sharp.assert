Run tests with coverage and then report missing branch coverage. 

Think if coverage is missing it either the tests are missing or 
there is a speculative code which is not needed and should be removed. 

If the uncovered code can not be reached via public api (Sharp.Assert or SharpInternal.AsssertXXX methods) 
it should be removed being either defensive programming or dead code  