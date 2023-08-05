using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Xesin.AddressablesExtensions
{
    public static class AsynOperationHandleExtensions
    {
        /// <summary>
        ///     Binds the lifetime of the handle to the <see cref="gameObject" />.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static AsyncOperationHandle BindTo(this AsyncOperationHandle self, GameObject gameObject)
        {
            if (gameObject == null)
            {
                Addressables.Release(self);
                throw new ArgumentNullException(nameof(gameObject),
                    $"{nameof(gameObject)} is null so the handle can't be bound and will be released immediately.");
            }

            if (!gameObject.TryGetComponent(out MonoBehaviourReleaseEvent releaseEvent))
                releaseEvent = gameObject.AddComponent<MonoBehaviourReleaseEvent>();

            return BindTo(self, releaseEvent);
        }

        /// <summary>
        ///     Binds the lifetime of the handle to the <see cref="gameObject" />.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="gameObject"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static AsyncOperationHandle<T> BindTo<T>(this AsyncOperationHandle<T> self, GameObject gameObject)
        {
            if (gameObject == null)
            {
                Addressables.Release(self);
                throw new ArgumentNullException(nameof(gameObject),
                    $"{nameof(gameObject)} is null so the handle can't be bound and will be released immediately.");
            }

            ((AsyncOperationHandle)self).BindTo(gameObject);
            return self;
        }

        /// <summary>
        ///     Binds the lifetime of the handle to the <see cref="releaseEvent" />.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="releaseEvent"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static AsyncOperationHandle BindTo(this AsyncOperationHandle self, IReleaseEvent releaseEvent)
        {
            if (releaseEvent == null)
            {
                Addressables.Release(self);
                throw new ArgumentNullException(nameof(releaseEvent),
                    $"{nameof(releaseEvent)} is null so the handle can't be bound and will be released immediately.");
            }

            void OnRelease()
            {
                Addressables.Release(self);
                releaseEvent.OnDispatch -= OnRelease;
            }

            releaseEvent.OnDispatch += OnRelease;
            return self;
        }

        /// <summary>
        ///     Binds the lifetime of the handle to the <see cref="releaseEvent" />.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="releaseEvent"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static AsyncOperationHandle<T> BindTo<T>(this AsyncOperationHandle<T> self, IReleaseEvent releaseEvent)
        {
            if (releaseEvent == null)
            {
                Addressables.Release(self);
                throw new ArgumentNullException(nameof(releaseEvent),
                    $"{nameof(releaseEvent)} is null so the handle can't be bound and will be released immediately.");
            }

            ((AsyncOperationHandle)self).BindTo(releaseEvent);
            return self;
        }
    }
}
