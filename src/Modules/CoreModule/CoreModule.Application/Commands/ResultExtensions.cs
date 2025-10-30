// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using System;

/// <summary>
/// Provides additional convenience extensions for synchronous and asynchronous
/// <see cref="Result{T}"/> operations. These methods simplify functional composition
/// by allowing in-place transformations without specifying generic input/output types.
/// </summary>
/// <remarks>
/// All methods preserve existing <see cref="Result{T}.Messages"/> and
/// <see cref="Result{T}.Errors"/> when successful. They automatically
/// convert thrown exceptions into <see cref="ExceptionError"/> results.
/// </remarks>
/// <example>
/// Typical usage:
/// <code>
/// var result = Result&lt;int&gt;.Success(2)
///     .Bind2(v => v * 10)      // returns 20
///     .Bind2(v => v + 1);      // returns 21
///
/// var asyncResult = await result.Bind2Async(async v =>
/// {
///     await Task.Delay(100);
///     return v * 2;
/// });
///
/// Console.WriteLine(asyncResult.Value); // 42
/// </code>
/// </example>
public static class ResultExtensions
{
    /// <summary>
    /// Applies a synchronous transformation function on a successful <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value inside the <see cref="Result{T}"/>.</typeparam>
    /// <param name="result">The input <see cref="Result{T}"/> instance.</param>
    /// <param name="func">A pure function that transforms the success value.</param>
    /// <returns>
    /// A new successful <see cref="Result{T}"/> containing the transformed value,
    /// or the original failure result if <paramref name="result"/> is a failure.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;string&gt;.Success("bravo")
    ///     .Bind2(v => v.ToUpper());
    ///
    /// // Result.Value = "BRAVO"
    /// </code>
    /// </example>
    public static Result<T> Bind<T>(
        this Result<T> result,
        Func<T, T> func)
    {
        if (result.IsFailure)
            return result;

        try
        {
            var newValue = func(result.Value);
            return Result<T>.Success(newValue)
                .WithMessages(result.Messages)
                .WithErrors(result.Errors);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure()
                .WithError(new ExceptionError(ex));
        }
    }

    /// <summary>
    /// Applies an asynchronous transformation function on a successful <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value inside the <see cref="Result{T}"/>.</typeparam>
    /// <param name="result">The input <see cref="Result{T}"/> instance.</param>
    /// <param name="func">An asynchronous function that transforms the success value.</param>
    /// <returns>
    /// A task returning a new <see cref="Result{T}"/> containing the transformed value,
    /// or the original failure result if <paramref name="result"/> is a failure.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;int&gt;.Success(10);
    ///
    /// var updated = await result.Bind2Async(async v =>
    /// {
    ///     await Task.Delay(50);
    ///     return v + 5;
    /// });
    ///
    /// // updated.Value == 15
    /// </code>
    /// </example>
    public static async Task<Result<T>> BindAsync<T>(
        this Result<T> result,
        Func<T, Task<T>> func)
    {
        if (result.IsFailure)
            return result;

        try
        {
            var newValue = await func(result.Value).AnyContext();
            return Result<T>.Success(newValue)
                .WithMessages(result.Messages)
                .WithErrors(result.Errors);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure()
                .WithError(new ExceptionError(ex));
        }
    }

    /// <summary>
    /// Applies a synchronous transformation function on an awaited <see cref="Task{TResult}"/><see cref="Result{T}"/> result.
    /// </summary>
    /// <typeparam name="T">The type of the encapsulated success value.</typeparam>
    /// <param name="task">An awaited <see cref="Task"/> returning a <see cref="Result{T}"/>.</param>
    /// <param name="func">A function that transforms the success value.</param>
    /// <returns>
    /// A task returning a new <see cref="Result{T}"/> containing the transformed value,
    /// or the original failure if the awaited result was unsuccessful.
    /// </returns>
    /// <example>
    /// <code>
    /// var resultTask = repository.FindResultAsync(id);
    ///
    /// var updated = await resultTask.Bind2(user =>
    /// {
    ///     user.LastLogin = DateTime.UtcNow;
    ///     return user;
    /// });
    /// </code>
    /// </example>
    public static async Task<Result<T>> Bind<T>(
        this Task<Result<T>> task,
        Func<T, T> func)
    {
        var result = await task.AnyContext();

        if (result.IsFailure)
            return result;

        try
        {
            var newValue = func(result.Value);
            return Result<T>.Success(newValue)
                .WithMessages(result.Messages)
                .WithErrors(result.Errors);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure()
                .WithError(new ExceptionError(ex));
        }
    }

    /// <summary>
    /// Applies an asynchronous transformation function on an awaited <see cref="Task{TResult}"/><see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the encapsulated success value.</typeparam>
    /// <param name="task">An awaited <see cref="Task"/> returning a <see cref="Result{T}"/>.</param>
    /// <param name="func">
    /// An asynchronous function that accepts the success value and a <see cref="CancellationToken"/>.
    /// </param>
    /// <param name="cancellationToken">An optional token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task returning a new <see cref="Result{T}"/> containing the transformed value,
    /// or the original failure if unsuccessful.
    /// </returns>
    /// <example>
    /// <code>
    /// var userResult = repository.GetUserResultAsync(id);
    ///
    /// var updated = await userResult.Bind2Async(async (user, ct) =>
    /// {
    ///     await emailService.SendNotificationAsync(user.Email, ct);
    ///     return user.WithLastNotified(DateTime.UtcNow);
    /// }, cancellationToken);
    /// </code>
    /// </example>
    public static async Task<Result<T>> BindAsync<T>(
        this Task<Result<T>> task,
        Func<T, CancellationToken, Task<T>> func,
        CancellationToken cancellationToken = default)
    {
        var result = await task.AnyContext();

        if (result.IsFailure)
            return result;

        try
        {
            var newValue = await func(result.Value, cancellationToken)
                .AnyContext();

            return Result<T>.Success(newValue);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure()
                .WithError(new ExceptionError(ex));
        }
    }

    /// <summary>
    /// Binds a synchronous operation that returns a <see cref="Result{TInner}"/> to a
    /// parent <see cref="Result{T}"/>, merging both contexts. If either result fails,
    /// the failure is propagated automatically.
    /// </summary>
    /// <typeparam name="T">Outer context type.</typeparam>
    /// <typeparam name="TInner">Inner success value type.</typeparam>
    /// <param name="result">The outer <see cref="Result{T}"/> to bind from.</param>
    /// <param name="func">
    /// A synchronous function returning another <see cref="Result{TInner}"/>.
    /// </param>
    /// <param name="merge">
    /// A function that merges <paramref name="result"/> and the inner success value.
    /// </param>
    /// <returns>
    /// A combined <see cref="Result{T}"/>; propagates any inner or outer failures.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;OrderContext&gt;.Success(ctx)
    ///     .BindResult(
    ///         ctx => inventory.CheckStock(ctx.ProductId),
    ///         (ctx, stock) => ctx.WithStock(stock));
    /// </code>
    /// </example>
    public static Result<T> BindResult<T, TInner>(
        this Result<T> result,
        Func<T, Result<TInner>> func,
        Func<T, TInner, T> merge)
    {
        if (result.IsFailure)
            return result;

        try
        {
            var inner = func(result.Value);
            if (inner.IsFailure)
                return inner.Wrap<T>();

            var combined = merge(result.Value, inner.Value);
            return Result<T>.Success(combined)
                .WithMessages(result.Messages)
                .WithErrors(result.Errors);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure()
                .WithError(new ExceptionError(ex));
        }
    }

    /// <summary>
    /// Binds an asynchronous operation that returns a <see cref="Result{TInner}"/> to
    /// a parent <see cref="Result{T}"/>, merging both contexts. If either fails, the
    /// failure is propagated automatically.
    /// </summary>
    /// <typeparam name="T">Outer context type.</typeparam>
    /// <typeparam name="TInner">Inner success value type.</typeparam>
    /// <param name="result">The outer <see cref="Result{T}"/>.</param>
    /// <param name="func">
    /// An async function returning <see cref="Result{TInner}"/> from <typeparamref name="T"/>.
    /// </param>
    /// <param name="merge">
    /// Function combining the outer and inner values into an updated
    /// <typeparamref name="T"/> instance.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A combined result with propagated failure support.</returns>
    /// <example>
    /// <code>
    /// var result = await Result&lt;CustomerContext&gt;.Success(ctx)
    ///     .BindResultAsync(
    ///         async (ctx, ct) => await numberGenerator.GetNextAsync("CustomerNumbers", "core", ct),
    ///         (ctx, seq) =>
    ///         {
    ///             ctx.Number = CustomerNumber.CreateNew(DateTime.UtcNow, seq);
    ///             return ctx;
    ///         },
    ///         cancellationToken);
    /// </code>
    /// </example>
    public static async Task<Result<T>> BindResultAsync<T, TInner>(
        this Result<T> result,
        Func<T, CancellationToken, Task<Result<TInner>>> func,
        Func<T, TInner, T> merge,
        CancellationToken cancellationToken = default)
    {
        if (result.IsFailure)
            return result;

        try
        {
            var inner = await func(result.Value, cancellationToken)
                .AnyContext();

            if (inner.IsFailure)
                return inner.Wrap<T>();

            var combined = merge(result.Value, inner.Value);
            return Result<T>.Success(combined)
                .WithMessages(result.Messages)
                .WithErrors(result.Errors);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure()
                .WithError(new ExceptionError(ex));
        }
    }

    /// <summary>
    /// Awaits a <see cref="Task{Result}"/> and binds a synchronous operation returning
    /// another <see cref="Result{TInner}"/>, propagating any failures.
    /// </summary>
    /// <typeparam name="T">Outer context type.</typeparam>
    /// <typeparam name="TInner">Inner success value type.</typeparam>
    /// <param name="task">Asynchronous source task returning a <see cref="Result{T}"/>.</param>
    /// <param name="func">Synchronous operation returning a <see cref="Result{TInner}"/>.</param>
    /// <param name="merge">Merge function combining outer and inner values.</param>
    /// <returns>
    /// A new <see cref="Task{TResult}"/> returning the combined <see cref="Result{T}"/>.
    /// </returns>
    public static async Task<Result<T>> BindResult<T, TInner>(
        this Task<Result<T>> task,
        Func<T, Result<TInner>> func,
        Func<T, TInner, T> merge)
    {
        var result = await task.AnyContext();

        return result.BindResult(func, merge);
    }

    /// <summary>
    /// Awaits a <see cref="Task{Result}"/> and binds an asynchronous operation returning
    /// another <see cref="Result{TInner}"/>, propagating any failures.
    /// </summary>
    /// <typeparam name="T">Outer context type.</typeparam>
    /// <typeparam name="TInner">Inner success value type.</typeparam>
    /// <param name="task">Source task returning a <see cref="Result{T}"/>.</param>
    /// <param name="func">Async operation returning a <see cref="Result{TInner}"/>.</param>
    /// <param name="merge">Merge function for successful results.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// A new <see cref="Task{TResult}"/> returning the combined <see cref="Result{T}"/>,
    /// propagating any failures automatically.
    /// </returns>
    public static async Task<Result<T>> BindResultAsync<T, TInner>(
        this Task<Result<T>> task,
        Func<T, CancellationToken, Task<Result<TInner>>> func,
        Func<T, TInner, T> merge,
        CancellationToken cancellationToken = default)
    {
        var result = await task.AnyContext();

        return await result
            .BindResultAsync(func, merge, cancellationToken).AnyContext();
    }
}