Use fresh subagent to implement next most important task in `prd.md` Instruct it to follow TDD strictly:
- write test
- then add minimal impl to make test compiles - stubs and hardcoded data are ok at this stage
- run the test again and check that it fails for expected reasons
- then add minimal real implementation to pass
- repeat
Subagent should respect DoD. Once task is done, subagent should mark it as completed in `prd.md` then commit. Commit messages should not include
increment numbering (like Increment 5: ..) but only contain the descriptive message as specified by increment title.  Then subagent should address
any quality issues post commit if any. 