
Claude Code and Bedrock Setup
-----------------------------

```bash
export AWS_PROFILE=claude-code

aws sso login

export ANTHROPIC_MODEL=arn:aws:bedrock:ap-southeast-2:772636757201:inference-profile/apac.anthropic.claude-sonnet-4-20250514-v1:0
export ANTHROPIC_SMALL_FAST_MODEL=arn:aws:bedrock:ap-southeast-2:772636757201:inference-profile/apac.anthropic.claude-3-haiku-20240307-v1:0
export AWS_PROFILE=claude-code
export AWS_REGION=ap-southeast-2
export CLAUDE_CODE_USE_BEDROCK=1

claude -c
```
