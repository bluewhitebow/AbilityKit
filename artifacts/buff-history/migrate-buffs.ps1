$map = @{
    'MobaBuffService.cs' = 'Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Services/Buffs/MobaBuffService.cs'
    'BuffLifecycleExecutor.cs' = 'Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Services/Buffs/Lifecycle/BuffLifecycleExecutor.cs'
    'BuffEventPublisher.cs' = 'Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Services/Buffs/BuffEventPublisher.cs'
    'BuffStageEffectExecutor.cs' = 'Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Services/Buffs/BuffStageEffectExecutor.cs'
    'BuffContextService.cs' = 'Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Services/Buffs/Core/BuffContextRegistry.cs'
    'BuffContinuousBindingService.cs' = 'Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Services/Buffs/Runtime/BuffContinuousBindingService.cs'
    'BuffContinuousIntervalHandler.cs' = 'Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Services/Buffs/Runtime/BuffContinuousIntervalHandler.cs'
    'MobaBuffPresentationCueReporter.cs' = 'Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Services/Buffs/Presentation/MobaBuffPresentationCueReporter.cs'
    'BuffRuntimeContexts.cs' = 'Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Services/Buffs/Core/BuffRuntimeContexts.cs'
    'BuffStackingPolicyApplier.cs' = 'Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Services/Buffs/Core/BuffStackingPolicyApplier.cs'
    'BuffContinuousRuntime.cs' = 'Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Services/Buffs/Runtime/BuffContinuousRuntime.cs'
}

foreach ($name in $map.Keys) {
    $src = Join-Path 'artifacts/buff-history' $name
    $dst = $map[$name]
    $text = Get-Content -Raw -Encoding UTF8 $src

    $text = $text -replace 'namespace AbilityKit\.Demo\.Moba\.Services\s*\{', 'namespace AbilityKit.Demo.Moba.Services.Buffs {'

    if ($name -eq 'BuffContextService.cs') {
        $text = $text -replace 'BuffContextService', 'BuffContextRegistry'
        $text = $text -replace 'namespace AbilityKit\.Demo\.Moba\.Services\.Buffs \{', 'namespace AbilityKit.Demo.Moba.Services.Buffs.Core {'
    }

    if ($name -in @('BuffRuntimeContexts.cs', 'BuffStackingPolicyApplier.cs')) {
        $text = $text -replace 'namespace AbilityKit\.Demo\.Moba\.Services\.Buffs \{', 'namespace AbilityKit.Demo.Moba.Services.Buffs.Core {'
    }

    if ($name -in @('BuffContinuousBindingService.cs', 'BuffContinuousIntervalHandler.cs', 'BuffContinuousRuntime.cs')) {
        $text = $text -replace 'namespace AbilityKit\.Demo\.Moba\.Services\.Buffs \{', 'namespace AbilityKit.Demo.Moba.Services.Buffs.Runtime {'
    }

    if ($name -eq 'BuffLifecycleExecutor.cs') {
        $text = $text -replace 'namespace AbilityKit\.Demo\.Moba\.Services\.Buffs \{', 'namespace AbilityKit.Demo.Moba.Services.Buffs.Lifecycle {'
    }

    if ($name -eq 'MobaBuffPresentationCueReporter.cs') {
        $text = $text -replace 'namespace AbilityKit\.Demo\.Moba\.Services\.Buffs \{', 'namespace AbilityKit.Demo.Moba.Services.Buffs.Presentation {'
    }

    $insert = New-Object System.Collections.Generic.List[string]
    if ($text -notmatch 'using AbilityKit\.Demo\.Moba\.Services;') { $insert.Add('using AbilityKit.Demo.Moba.Services;') }
    if ($text -notmatch 'using AbilityKit\.Demo\.Moba\.Services\.Buffs\.Core;' -and $name -notin @('BuffRuntimeContexts.cs', 'BuffStackingPolicyApplier.cs', 'BuffContextService.cs')) { $insert.Add('using AbilityKit.Demo.Moba.Services.Buffs.Core;') }
    if ($text -notmatch 'using AbilityKit\.Demo\.Moba\.Services\.Buffs\.Runtime;' -and $name -notin @('BuffContinuousBindingService.cs', 'BuffContinuousIntervalHandler.cs', 'BuffContinuousRuntime.cs')) { $insert.Add('using AbilityKit.Demo.Moba.Services.Buffs.Runtime;') }
    if ($text -notmatch 'using AbilityKit\.Demo\.Moba\.Services\.Buffs\.Presentation;' -and $name -ne 'MobaBuffPresentationCueReporter.cs') { $insert.Add('using AbilityKit.Demo.Moba.Services.Buffs.Presentation;') }
    if ($text -notmatch 'using AbilityKit\.Demo\.Moba\.Services\.Buffs\.Triggering;') { $insert.Add('using AbilityKit.Demo.Moba.Services.Buffs.Triggering;') }

    if ($insert.Count -gt 0) {
        $prefix = [string]::Join("`r`n", $insert.ToArray()) + "`r`n`r`n"
        $text = $text -replace '(?m)^(namespace )', ($prefix + '$1')
    }

    Set-Content -Encoding UTF8 $dst $text
}
